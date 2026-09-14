using IntelliDocs.Core.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliDocs.Infrastructure.Persistence.Configurations;

public sealed class DocumentAnalysisRecordConfiguration
    : IEntityTypeConfiguration<DocumentAnalysisRecord>
{
    public void Configure(
        EntityTypeBuilder<DocumentAnalysisRecord> builder)
    {
        builder.ToTable("document_analysis_records");

        builder.HasKey(x => x.DocumentId);

        builder.Property(x => x.DocumentId)
            .HasColumnName("document_id")
            .ValueGeneratedNever();

        builder.Property(x => x.ModelId)
            .HasColumnName("model_id")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ModelVersion)
            .HasColumnName("model_version")
            .HasMaxLength(100);

        builder.Property(x => x.ResultJson)
            .HasColumnName("result_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.AnalyzedAtUtc)
            .HasColumnName("analyzed_at_utc")
            .IsRequired();

        builder.HasOne<DocumentJob>()
            .WithOne()
            .HasForeignKey<DocumentAnalysisRecord>(
                x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}