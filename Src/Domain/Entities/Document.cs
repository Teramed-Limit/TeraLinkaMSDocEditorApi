namespace TeraLinkaMSDocEditorApi.Domain.Entities;

public partial class Document
{
    public string Id { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public string FileType { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime LastModifiedAt { get; set; }

    public string? LastModifiedBy { get; set; }

    public bool? IsTemplate { get; set; }
}
