using IntelliDocs.Core.Documents;
using IntelliDocs.Core.Reviews;
using Microsoft.EntityFrameworkCore;

namespace IntelliDocs.Infrastructure.Persistence;

public sealed class IntelliDocsDbContext : DbContext
{
    public IntelliDocsDbContext(
        DbContextOptions<IntelliDocsDbContext> options)
        : base(options)
    {
    }

    public DbSet<DocumentJob> DocumentJobs =>
        Set<DocumentJob>();

    public DbSet<DocumentJobTransition> DocumentJobTransitions =>
        Set<DocumentJobTransition>();

    public DbSet<DocumentAnalysisRecord> DocumentAnalysisRecords =>
        Set<DocumentAnalysisRecord>();

    public DbSet<DocumentReview> DocumentReviews =>
        Set<DocumentReview>();

    public DbSet<DocumentReviewCorrection> DocumentReviewCorrections =>
        Set<DocumentReviewCorrection>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(IntelliDocsDbContext).Assembly);
    }
}