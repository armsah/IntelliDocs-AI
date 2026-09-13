using IntelliDocs.Core.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliDocs.Infrastructure.Persistence.Configurations;

public sealed class DocumentJobTransitionConfiguration
    : IEntityTypeConfiguration<DocumentJobTransition>
{
    public void Configure(
        EntityTypeBuilder<DocumentJobTransition> builder)
    {
        builder.ToTable("document_job_transitions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(x => x.DocumentId)
            .HasColumnName("document_id")
            .IsRequired();

        builder.Property(x => x.PreviousStatus)
            .HasColumnName("previous_status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.NextStatus)
            .HasColumnName("next_status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .IsRequired();

        builder.Property(x => x.Actor)
            .HasColumnName("actor")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.ProcessingStage)
            .HasColumnName("processing_stage")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasColumnName("reason")
            .HasMaxLength(2000);

        builder.HasIndex(x => new
            {
                x.DocumentId,
                x.OccurredAtUtc
            })
            .HasDatabaseName(
                "ix_document_job_transitions_document_time");
    }
}

