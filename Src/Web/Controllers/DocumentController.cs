using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeraLinkaMSDocEditorApi.Application.DTOs;
using TeraLinkaMSDocEditorApi.Application.Services;
using TeraLinkaMSDocEditorApi.Web.Extensions;

namespace TeraLinkaMSDocEditorApi.Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class DocumentController : ControllerBase
{
    private readonly DocumentService _documentService;

    public DocumentController(DocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetDocuments()
    {
        try
        {
            var documents = await _documentService.GetDocuments();
            return Ok(documents);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = true, message = "獲取文檔列表時發生錯誤" });
        }
    }

    [HttpGet("templates")]
    public async Task<ActionResult<IEnumerable<DocumentDto>>> GetDocumentTemplates()
    {
        try
        {
            var documents = await _documentService.GetDocuments();
            return Ok(documents.Where(x => x.IsTemplate == true));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = true, message = "獲取文檔列表時發生錯誤" });
        }
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetEditorConfig(
        string id,
        string mode,
        string? templateId = null,
        string? fileName = null,
        bool? forceSwitchTemplate = false)
    {
        try
        {
            if (!Enum.TryParse<DocumentMode>(mode, true, out var documentMode))
                return BadRequest($"無效的mode參數: {mode}");

            var userId = User.Identity?.Name;

            object config;
            var document = await _documentService.GetDocument(id);
            if (forceSwitchTemplate == true || document == null && !string.IsNullOrEmpty(templateId))
            {
                var newDocId = await _documentService.CreateDocumentFromTemplate(id, fileName, templateId);
                config = await _documentService.GetEditorConfig(id, DocumentMode.Edit, userId ?? string.Empty);
            }
            else
            {
                config = await _documentService.GetEditorConfig(id, documentMode, userId ?? string.Empty);
            }

            return Ok(config);
        }
        catch (FileNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id}/rename/{fileName}")]
    public async Task<IActionResult> RenameFile(string id, string fileName)
    {
        try
        {
            await _documentService.RenameFile(id, fileName);
            return Ok(new { error = 0 });
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = 1, message = "找不到指定的文檔" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = 1, message = "處理回調時發生內部錯誤" });
        }
    }

    [HttpPost("{id}/callback")]
    public async Task<IActionResult> SaveCallback(string id)
    {
        try
        {
            await _documentService.ProcessCallback(id, Request.Body);
            return Ok(new { error = 0 });
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = 1, message = "找不到指定的文檔" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = 1, message = "處理回調時發生內部錯誤" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDocument(string id)
    {
        try
        {
            await _documentService.DeleteDocument(id);
            return Ok(new { error = 0 });
        }
        catch (FileNotFoundException)
        {
            return NotFound(new { error = 1, message = "找不到指定的文檔" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = 1, message = "處理時發生內部錯誤" });
        }
    }
}