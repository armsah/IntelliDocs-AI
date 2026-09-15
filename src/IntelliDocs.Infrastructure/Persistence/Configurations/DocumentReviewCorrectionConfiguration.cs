using IntelliDocs.Core.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliDocs.Infrastructure.Persistence.Configurations;

public sealed class DocumentReviewCorrectionConfiguration
    : IEntityTypeConfiguration<DocumentReviewCorrection>
{
    public void Configure(
        EntityTypeBuilder<DocumentReviewCorrection> builder)
    {
        builder.ToTable("document_review_corrections");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.ReviewId)
            .HasColumnName("review_id")
            .IsRequired();

        builder.Property(x => x.FieldName)
            .HasColumnName("field_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.OriginalValue)
            .HasColumnName("original_value")
            .HasColumnType("text");

        builder.Property(x => x.CorrectedValue)
            .HasColumnName("corrected_value")
            .HasColumnType("text");

        builder.Property(x => x.Reviewer)
            .HasColumnName("reviewer")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(x => x.CorrectedAtUtc)
            .HasColumnName("corrected_at_utc")
            .IsRequired();

        builder.HasIndex(x => new
            {
                x.ReviewId,
                x.CorrectedAtUtc
            })
            .HasDatabaseName(
                "ix_document_review_corrections_review_time");
    }
}
