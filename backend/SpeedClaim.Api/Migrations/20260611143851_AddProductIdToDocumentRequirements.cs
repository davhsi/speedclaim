using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProductIdToDocumentRequirements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "product_id",
                table: "document_requirements",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "document_requirements",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "product_id",
                value: null);

            migrationBuilder.UpdateData(
                table: "document_requirements",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "product_id",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_document_requirements_product_id",
                table: "document_requirements",
                column: "product_id");

            migrationBuilder.AddForeignKey(
                name: "FK_document_requirements_insurance_products_ProductId",
                table: "document_requirements",
                column: "product_id",
                principalTable: "insurance_products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_document_requirements_insurance_products_ProductId",
                table: "document_requirements");

            migrationBuilder.DropIndex(
                name: "IX_document_requirements_product_id",
                table: "document_requirements");

            migrationBuilder.DropColumn(
                name: "product_id",
                table: "document_requirements");
        }
    }
}
