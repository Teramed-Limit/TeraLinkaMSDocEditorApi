using System.ComponentModel;

namespace TeraLinkaMSDocEditorApi.Application.DTOs;

public class CreateDocumentRequest
{
    public string? Title { get; set; }
}

public enum DocumentMode
{
    [Description("Edit")] Edit,
    [Description("FillForms")] FillForms
}

public class DocumentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public string FileType { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public string? LastModifiedBy { get; set; }
}