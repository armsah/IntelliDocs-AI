using IntelliDocs.Core.Documents;
using IntelliDocs.Core.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliDocs.Infrastructure.Persistence.Configurations;

public sealed class DocumentReviewConfiguration
    : IEntityTypeConfiguration<DocumentReview>
{
    public void Configure(
        EntityTypeBuilder<DocumentReview> builder)
    {
        builder.ToTable("document_reviews");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.DocumentId)
            .HasColumnName("document_id")
            .IsRequired();

        builder.Property(x => x.Reviewer)
            .HasColumnName("reviewer")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(x => x.StartedAtUtc)
            .HasColumnName("started_at_utc")
            .IsRequired();

        builder.Property(x => x.Decision)
            .HasColumnName("decision")
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.DecisionReason)
            .HasColumnName("decision_reason")
            .HasMaxLength(2000);

        builder.Property(x => x.DecidedAtUtc)
            .HasColumnName("decided_at_utc");

        builder.Ignore(x => x.IsCompleted);

        builder.HasIndex(x => x.DocumentId)
            .IsUnique()
            .HasDatabaseName(
                "ix_document_reviews_document_id");

        builder.HasOne<DocumentJob>()
            .WithOne()
            .HasForeignKey<DocumentReview>(
                x => x.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Corrections)
            .WithOne()
            .HasForeignKey(x => x.ReviewId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Corrections)
            .HasField("_corrections")
            .UsePropertyAccessMode(
                PropertyAccessMode.Field);
    }
}
