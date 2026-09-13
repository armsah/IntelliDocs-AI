using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliDocs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueDocumentContentIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_document_jobs_tenant_sha256",
                table: "document_jobs");

            migrationBuilder.CreateIndex(
                name: "ix_document_jobs_tenant_sha256",
                table: "document_jobs",
                columns: new[] { "tenant_id", "sha256" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_document_jobs_tenant_sha256",
                table: "document_jobs");

            migrationBuilder.CreateIndex(
                name: "ix_document_jobs_tenant_sha256",
                table: "document_jobs",
                columns: new[] { "tenant_id", "sha256" });
        }
    }
}
