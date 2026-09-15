using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IntelliDocs.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentReviewAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "document_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    decision = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    decision_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    decided_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_reviews", x => x.id);
                    table.ForeignKey(
                        name: "FK_document_reviews_document_jobs_document_id",
                        column: x => x.document_id,
                        principalTable: "document_jobs",
                        principalColumn: "document_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_review_corrections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    original_value = table.Column<string>(type: "text", nullable: true),
                    corrected_value = table.Column<string>(type: "text", nullable: true),
                    reviewer = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    corrected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_review_corrections", x => x.id);
                    table.ForeignKey(
                        name: "FK_document_review_corrections_document_reviews_review_id",
                        column: x => x.review_id,
                        principalTable: "document_reviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_document_review_corrections_review_time",
                table: "document_review_corrections",
                columns: new[] { "review_id", "corrected_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_document_reviews_document_id",
                table: "document_reviews",
                column: "document_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_review_corrections");

            migrationBuilder.DropTable(
                name: "document_reviews");
        }
    }
}
