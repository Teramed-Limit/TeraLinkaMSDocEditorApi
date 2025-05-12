using Microsoft.EntityFrameworkCore;
using TeraLinkaMSDocEditorApi.Domain.Entities;

namespace TeraLinkaMSDocEditorApi.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        // 確保資料庫在運行時自動創建
        // Database.EnsureCreated();
    }

    public DbSet<Document> Documents { get; set; }
    public DbSet<DocumentTemplate> DocumentTemplates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 配置 Document 實體
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FileType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(100);
        });

        // 配置 DocumentTemplate 實體
        modelBuilder.Entity<DocumentTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.TemplatePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FileType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(100);
        });
    }
}