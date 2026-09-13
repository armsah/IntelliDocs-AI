using IntelliDocs.Core.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliDocs.Infrastructure.Persistence.Configurations;

public sealed class DocumentJobConfiguration
    : IEntityTypeConfiguration<DocumentJob>
{
    public void Configure(
        EntityTypeBuilder<DocumentJob> builder)
    {
        builder.ToTable("document_jobs");

        builder.HasKey(x => x.DocumentId);

        builder.Property(x => x.DocumentId)
            .HasColumnName("document_id")
            .ValueGeneratedNever();

        builder.Property(x => x.TenantId)
            .HasColumnName("tenant_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.OriginalFileName)
            .HasColumnName("original_file_name")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.OriginalStorageUri)
            .HasColumnName("original_storage_uri")
            .HasMaxLength(2000);

        builder.Property(x => x.Sha256)
            .HasColumnName("sha256")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.DetectedType)
            .HasColumnName("detected_type")
            .HasMaxLength(100);

        builder.Property(x => x.AiModelId)
            .HasColumnName("ai_model_id")
            .HasMaxLength(200);

        builder.Property(x => x.AiModelVersion)
            .HasColumnName("ai_model_version")
            .HasMaxLength(100);

        builder.Property(x => x.ProcessingStatus)
            .HasColumnName("processing_status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.SubmittedAtUtc)
            .HasColumnName("submitted_at_utc")
            .IsRequired();

        builder.Property(x => x.CompletedAtUtc)
            .HasColumnName("completed_at_utc");

        builder.HasIndex(x => new
            {
                x.TenantId,
                x.Sha256
            })
            .IsUnique()
            .HasDatabaseName(
                "ix_document_jobs_tenant_sha256");

        builder.HasIndex(x => x.ProcessingStatus)
            .HasDatabaseName(
                "ix_document_jobs_processing_status");

        builder.HasMany(x => x.Transitions)
            .WithOne()
            .HasForeignKey(x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Transitions)
            .HasField("_transitions")
            .UsePropertyAccessMode(
                PropertyAccessMode.Field);
    }
}
