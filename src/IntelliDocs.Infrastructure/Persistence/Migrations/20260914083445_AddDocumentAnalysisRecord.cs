using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliDocs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentAnalysisRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "document_analysis_records",
                columns: table => new
                {
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    model_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    result_json = table.Column<string>(type: "jsonb", nullable: false),
                    analyzed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_analysis_records", x => x.document_id);
                    table.ForeignKey(
                        name: "FK_document_analysis_records_document_jobs_document_id",
                        column: x => x.document_id,
                        principalTable: "document_jobs",
                        principalColumn: "document_id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_analysis_records");
        }
    }
}
