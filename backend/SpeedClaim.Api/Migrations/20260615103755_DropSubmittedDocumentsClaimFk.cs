using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class DropSubmittedDocumentsClaimFk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_submitted_documents_claims_claim_id",
                table: "submitted_documents");

            migrationBuilder.DropIndex(
                name: "IX_submitted_documents_entity_id",
                table: "submitted_documents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_submitted_documents_entity_id",
                table: "submitted_documents",
                column: "entity_id");

            migrationBuilder.AddForeignKey(
                name: "FK_submitted_documents_claims_claim_id",
                table: "submitted_documents",
                column: "entity_id",
                principalTable: "claims",
                principalColumn: "id");
        }
    }
}
