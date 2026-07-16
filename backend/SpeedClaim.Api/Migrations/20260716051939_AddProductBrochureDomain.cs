using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProductBrochureDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_brochures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    original_filename = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    blob_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_size_kb = table.Column<int>(type: "integer", nullable: false),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: false),
                    effective_to = table.Column<DateOnly>(type: "date", nullable: true),
                    page_count = table.Column<int>(type: "integer", nullable: true),
                    parent_chunk_count = table.Column<int>(type: "integer", nullable: true),
                    child_chunk_count = table.Column<int>(type: "integer", nullable: true),
                    embedding_provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    embedding_model = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    embedding_dimension = table.Column<int>(type: "integer", nullable: true),
                    ingestion_error_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    published_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_brochures", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_brochures_products_product_id",
                        column: x => x.product_id,
                        principalTable: "insurance_products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_brochures_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_product_brochures_users_published_by_id",
                        column: x => x.published_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_product_brochures_created_by_id",
                table: "product_brochures",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_brochures_published_by_id",
                table: "product_brochures",
                column: "published_by_id");

            migrationBuilder.CreateIndex(
                name: "uq_product_brochures_current_published",
                table: "product_brochures",
                column: "product_id",
                unique: true,
                filter: "\"status\" = 'Published'");

            migrationBuilder.CreateIndex(
                name: "uq_product_brochures_product_content_hash",
                table: "product_brochures",
                columns: new[] { "product_id", "content_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_product_brochures_product_version",
                table: "product_brochures",
                columns: new[] { "product_id", "version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_brochures");
        }
    }
}
