using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveKycBackDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "aadhaar_document_key_back",
                table: "kyc_records");

            migrationBuilder.DropColumn(
                name: "pan_document_key_back",
                table: "kyc_records");

            migrationBuilder.RenameColumn(
                name: "aadhaar_document_key_front",
                table: "kyc_records",
                newName: "aadhaar_document_key");

            migrationBuilder.RenameColumn(
                name: "pan_document_key_front",
                table: "kyc_records",
                newName: "pan_document_key");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "aadhaar_document_key",
                table: "kyc_records",
                newName: "aadhaar_document_key_front");

            migrationBuilder.RenameColumn(
                name: "pan_document_key",
                table: "kyc_records",
                newName: "pan_document_key_front");

            migrationBuilder.AddColumn<string>(
                name: "aadhaar_document_key_back",
                table: "kyc_records",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pan_document_key_back",
                table: "kyc_records",
                type: "text",
                nullable: true);
        }
    }
}
