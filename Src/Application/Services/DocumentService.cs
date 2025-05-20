using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TeraLinkaMSDocEditorApi.Application.Common.Utils;
using TeraLinkaMSDocEditorApi.Application.DTOs;
using TeraLinkaMSDocEditorApi.Domain.Entities;
using TeraLinkaMSDocEditorApi.Infrastructure.Persistence;
using TeraLinkaMSDocEditorApi.Web.Hubs;

namespace TeraLinkaMSDocEditorApi.Application.Services;

public class DocumentService
{
    private readonly string _storagePath;
    private readonly string _docHttpUrl;
    private readonly string _selfHostedUrl;
    private readonly string _language;
    private readonly ILogger<DocumentService> _logger;
    private readonly IHubContext<DocumentHub> _hubContext;
    private readonly ApplicationDbContext _context;

    public DocumentService(
        IConfiguration configuration,
        ILogger<DocumentService> logger,
        IHubContext<DocumentHub> hubContext,
        ApplicationDbContext context)
    {
        _storagePath = configuration.GetSection("DocStoragePath").Value;
        _docHttpUrl = configuration.GetSection("DocStorageUrl").Value;
        _selfHostedUrl = configuration.GetSection("SelfHostedUrl").Value;
        _language = configuration.GetSection("Language").Value;
        _logger = logger;
        _hubContext = hubContext;
        _context = context;

        if (!Directory.Exists(_storagePath))
            Directory.CreateDirectory(_storagePath);
    }

    public async Task<List<Document>> GetDocuments()
    {
        var documents = await _context.Documents.ToListAsync();

        return documents;
    }

    public async Task<Document?> GetDocument(string id)
    {
        var documents = await _context.Documents
            .FirstOrDefaultAsync((x) => x.Id.ToString() == id);

        return documents;
    }

    public async Task<bool> DeleteDocument(string id)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(x => x.Id.ToString() == id);

        if (document == null)
            return false;

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();

        return true;
    }


    private async Task<Document> UpsertDocument(string id, string fileName)
    {
        var document = await GetDocument(id);
        var docFileName = string.IsNullOrEmpty(fileName) ? id : fileName;

        if (document == null)
        {
            document = new Document
            {
                Id = id,
                FilePath = Path.Combine(_storagePath, docFileName + ".docx"),
                FileName = docFileName + ".docx",
                FileType = "docx",
                CreatedBy = "",
                CreatedAt = DateTime.UtcNow,
                LastModifiedBy = "",
                LastModifiedAt = DateTime.UtcNow,
                // isTemplate = isTemplate
            };
            await _context.Documents.AddAsync(document);
        }
        else
        {
            document.LastModifiedAt = DateTime.UtcNow;
            document.LastModifiedBy = "";
            _context.Documents.Update(document);
        }

        await _context.SaveChangesAsync();
        return document;
    }

    public async Task<Document> RenameFile(string id, string newFileName)
    {
        var document = await GetDocument(id);
        if (document == null)
            throw new FileNotFoundException($"找不到ID為 {id} 的文檔");

        var newFileNameWithExt = newFileName + "." + document.FileType;
        document.FilePath = Path.Combine(_storagePath, newFileNameWithExt);
        document.FileName = newFileNameWithExt;
        _context.Documents.Update(document);
        await _context.SaveChangesAsync();
        return document;
    }

    public async Task<object> GetEditorConfig(string id, DocumentMode mode, string userId)
    {
        var document = await GetDocument(id);

        if (document == null)
            throw new FileNotFoundException($"找不到ID為 {id} 的文檔");

        var filePath = document.FilePath;
        var fileNameExt = document.FileType;
        var fileName = document.FileName;
        var fileHttpUrl = Path.Combine(_docHttpUrl, fileName);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"找不到文檔");

        var documentKey = DocumentUtils.GenerateDocumentKey(fileName, id);

        return new
        {
            Document = new
            {
                DocId = document.Id,
                FileType = fileNameExt,
                Key = documentKey,
                Title = fileName,
                Url = fileHttpUrl,
                Permissions = new
                {
                    Download = true,
                    Print = true,
                    Copy = true,
                    Edit = mode == DocumentMode.Edit,
                    Review = mode == DocumentMode.Edit,
                    Comment = mode == DocumentMode.Edit
                }
            },
            DocumentType = DocumentUtils.GetDocumentTypeByFileType(fileNameExt),
            Type = "desktop",
            EditorConfig = new
            {
                CallbackUrl = $"{_selfHostedUrl}/Document/{id}/callback",
                Mode = "edit",
                Lang = _language,
                Region = _language,
                User = new { Id = userId, Name = userId },
                Customization = new { Forcesave = true, },
                CoEditing = new { Mode = "Strict" },
            }
        };
    }

    public async Task ProcessCallback(string id, Stream requestBody)
    {
        try
        {
            using var reader = new StreamReader(requestBody);
            var body = await reader.ReadToEndAsync();
            var callbackJson = JsonDocument.Parse(body);
            var root = callbackJson.RootElement;

            var docKey = root.GetProperty("key").GetString();
            var status = root.GetProperty("status").GetInt32();


            var doc = await GetDocument(id);
            if (doc == null)
            {
                _logger.LogError($"找不到ID為 {id} 的文檔");
                throw new FileNotFoundException($"找不到ID為 {id} 的文檔");
            }

            if (!File.Exists(doc.FilePath))
            {
                _logger.LogError($"文檔 {id} 的文件不存在: {doc.FileName}");
                throw new FileNotFoundException($"文檔 {id} 的文件不存在: {doc.FileName}");
            }

            if (status == 6)
                await _hubContext.Clients.All.SendAsync("ReceiveSaveStatus", id, "saving");

            await ProcessCallbackStatus(status, root, doc.FilePath, doc.FileName);

            await UpsertDocument(id, doc.FileName);

            // 儲存成功後發送通知
            if (status == 6)
                await _hubContext.Clients.All.SendAsync("ReceiveSaveStatus", id, "saved");
        }
        catch (Exception ex)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveSaveStatus", id, "error");
            throw;
        }
    }

    private async Task ProcessCallbackStatus(int status, JsonElement root, string filePath, string fileName)
    {
        switch (status)
        {
            case 1: // 用戶連接或斷開文檔協作
                await ProcessUserConnection(root, fileName);
                break;
            case 2: // 文檔準備保存
                // await ProcessDocumentSave(root, filePath, fileName);
                break;
            case 3: // 文檔保存錯誤
                _logger.LogError($"文檔 {fileName} 保存失敗");
                break;
            case 4: // 文檔關閉無更改
                _logger.LogInformation($"文檔 {fileName} 已關閉，無修改");
                break;
            case 6: // 強制保存
                await ProcessForceSave(root, filePath, fileName);
                break;
            case 7: // 強制保存錯誤
                _logger.LogError($"文檔 {fileName} 強制保存失敗");
                break;
            default:
                _logger.LogWarning($"未知狀態: {status}");
                break;
        }
    }

    private async Task ProcessUserConnection(JsonElement root, string fileName)
    {
        if (root.TryGetProperty("actions", out var actionsElement))
        {
            foreach (var action in actionsElement.EnumerateArray())
            {
                var actionType = action.GetProperty("type").GetInt32();
                var userId = action.GetProperty("userid").GetString();
                _logger.LogInformation($"文檔 {fileName}: 用戶 {userId} 執行了動作類型 {actionType}");
            }
        }
    }

    private async Task ProcessDocumentSave(JsonElement root, string filePath, string fileName)
    {
        await SaveDocumentFromUrl(root, filePath);

        if (root.TryGetProperty("history", out var historyElement))
        {
            await SaveDocumentHistory(historyElement, fileName);
        }

        if (root.TryGetProperty("users", out var usersElement))
        {
            var lastEditUser = usersElement.EnumerateArray().FirstOrDefault().GetString();
            _logger.LogInformation($"文檔 {fileName} 由用戶 {lastEditUser} 最後編輯");
        }
    }

    private async Task ProcessForceSave(JsonElement root, string filePath, string fileName)
    {
        if (root.TryGetProperty("forcesavetype", out var forceSaveTypeElement))
        {
            var forceSaveType = forceSaveTypeElement.GetInt32();
            await ProcessForceSaveType(forceSaveType, root);
        }

        // 表單提交，要更換名字
        if (forceSaveTypeElement.GetInt32() == 3)
        {
            var extension = Path.GetExtension(filePath);
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            var newFilePath = Path.Combine(_storagePath, $"{fileNameWithoutExtension}_form{extension}");
            await SaveDocumentFromUrl(root, newFilePath);
            return;
        }

        await SaveDocumentFromUrl(root, filePath);
    }

    private async Task ProcessForceSaveType(int forceSaveType, JsonElement root)
    {
        switch (forceSaveType)
        {
            case 3: // 表單提交
                if (root.TryGetProperty("formsdataurl", out var formsDataUrlElement))
                {
                    var formsDataUrl = formsDataUrlElement.GetString();
                    await ProcessFormData(formsDataUrl);
                }

                break;
        }
    }

    private async Task SaveDocumentFromUrl(JsonElement root, string filePath)
    {
        if (root.TryGetProperty("url", out var urlElement))
        {
            var downloadUrl = urlElement.GetString();
            using var httpClient = new HttpClient();
            var fileBytes = await httpClient.GetByteArrayAsync(downloadUrl);
            await System.IO.File.WriteAllBytesAsync(filePath, fileBytes);

            if (root.TryGetProperty("changesurl", out var changesUrlElement))
            {
                var changesUrl = changesUrlElement.GetString();
                await SaveChangesHistory(changesUrl, filePath);
            }
        }
    }

    private async Task SaveDocumentHistory(JsonElement historyElement, string fileName)
    {
        // TODO: 實現歷史記錄保存邏輯
        _logger.LogInformation($"保存文檔 {fileName} 的歷史記錄");
    }

    private async Task SaveChangesHistory(string changesUrl, string filePath)
    {
        try
        {
            var historyDir = Path.Combine(Path.GetDirectoryName(filePath), "history");
            if (!Directory.Exists(historyDir))
            {
                Directory.CreateDirectory(historyDir);
            }

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var historyFilePath = Path.Combine(historyDir, $"{fileName}_{timestamp}.zip");

            using var httpClient = new HttpClient();
            var fileBytes = await httpClient.GetByteArrayAsync(changesUrl);
            await System.IO.File.WriteAllBytesAsync(historyFilePath, fileBytes);

            _logger.LogInformation($"已保存文檔變更歷史至 {historyFilePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存變更歷史時發生錯誤");
        }
    }

    public async Task<string> CreateDocumentFromTemplate(string newDocId, string? fileName, string templateId)
    {
        var templateDoc = await GetDocument(templateId);
        if (templateDoc == null)
            throw new FileNotFoundException($"找不到模板文檔: {templateId}");

        var newDoc = await UpsertDocument(newDocId, fileName);
        File.Copy(templateDoc.FilePath, Path.Combine(_storagePath, newDoc.FilePath), true);
        return newDocId;
    }

    private async Task ProcessFormData(string formsDataUrl)
    {
        try
        {
            // using var httpClient = new HttpClient();D
            // var jsonString = await httpClient.GetStringAsync(formsDataUrl);
            // var formData = JsonDocument.Parse(jsonString);

            // foreach (var formField in formData.RootElement.EnumerateArray())
            // {
            //     var key = formField.GetProperty("key").GetString();
            //     var tag = formField.GetProperty("tag").GetString();
            //     var value = formField.GetProperty("value").GetString();
            //     var type = formField.GetProperty("type").GetString();
            //
            //     await ProcessFormField(type, key, value, tag);
            // }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "處理表單數據時發生錯誤");
            throw;
        }
    }
}