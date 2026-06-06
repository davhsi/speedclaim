using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDocumentClaimRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_documents_claims_claim_id",
                table: "documents");

            migrationBuilder.DropForeignKey(
                name: "FK_documents_policies_policy_id",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_documents_policy_id",
                table: "documents");

            migrationBuilder.AlterColumn<decimal>(
                name: "risk_score",
                table: "claims",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0.00m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.InsertData(
                table: "document_types",
                columns: new[] { "id", "code", "domain", "is_sensitive_phi_pii", "name" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000005"), "SUPPORTING_DOC", "HEALTH", true, "Supporting Document" },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "SUPPORTING_DOC", "VEHICLE", false, "Supporting Document" },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "SUPPORTING_DOC", "LIFE", true, "Supporting Document" }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_documents_claims_claim_id",
                table: "documents",
                column: "claim_id",
                principalTable: "claims",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_documents_claims_claim_id",
                table: "documents");

            migrationBuilder.DeleteData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "document_types",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000007"));

            migrationBuilder.AlterColumn<decimal>(
                name: "risk_score",
                table: "claims",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldDefaultValue: 0.00m);

            migrationBuilder.CreateIndex(
                name: "IX_documents_policy_id",
                table: "documents",
                column: "policy_id");

            migrationBuilder.AddForeignKey(
                name: "FK_documents_claims_claim_id",
                table: "documents",
                column: "claim_id",
                principalTable: "claims",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_documents_policies_policy_id",
                table: "documents",
                column: "policy_id",
                principalTable: "policies",
                principalColumn: "id");
        }
    }
}
