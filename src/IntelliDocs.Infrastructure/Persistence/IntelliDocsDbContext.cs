using IntelliDocs.Core.Documents;
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

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(IntelliDocsDbContext).Assembly);
    }
}