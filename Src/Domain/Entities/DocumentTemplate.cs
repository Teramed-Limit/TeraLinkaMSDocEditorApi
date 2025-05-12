using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TeraLinkaMSDocEditorApi.Domain.Entities;

[Table("DocumentTemplates")]
public class DocumentTemplate
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; }

    [Required]
    [MaxLength(500)]
    public string TemplatePath { get; set; }

    [Required]
    [MaxLength(50)]
    public string FileType { get; set; } // 例如: "docx", "xlsx", "pptx"

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    [MaxLength(100)]
    public string CreatedBy { get; set; }

    [Required]
    public DateTime LastModifiedAt { get; set; }

    [MaxLength(100)]
    public string? LastModifiedBy { get; set; }
}