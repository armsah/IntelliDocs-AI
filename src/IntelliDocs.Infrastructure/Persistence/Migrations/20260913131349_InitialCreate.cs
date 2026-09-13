using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliDocs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "document_jobs",
                columns: table => new
                {
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    original_storage_uri = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    detected_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ai_model_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ai_model_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    processing_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    submitted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_jobs", x => x.document_id);
                });

            migrationBuilder.CreateTable(
                name: "document_job_transitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    next_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    processing_stage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_job_transitions", x => x.id);
                    table.ForeignKey(
                        name: "FK_document_job_transitions_document_jobs_document_id",
                        column: x => x.document_id,
                        principalTable: "document_jobs",
                        principalColumn: "document_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_document_job_transitions_document_time",
                table: "document_job_transitions",
                columns: new[] { "document_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_document_jobs_processing_status",
                table: "document_jobs",
                column: "processing_status");

            migrationBuilder.CreateIndex(
                name: "ix_document_jobs_tenant_sha256",
                table: "document_jobs",
                columns: new[] { "tenant_id", "sha256" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_job_transitions");

            migrationBuilder.DropTable(
                name: "document_jobs");
        }
    }
}
