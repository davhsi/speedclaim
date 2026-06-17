using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class RefactorKycToDualDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_kyc_records_id_type_number",
                table: "kyc_records");

            migrationBuilder.DeleteData(
                table: "kyc_records",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.DropColumn(
                name: "id_number",
                table: "kyc_records");

            migrationBuilder.DropColumn(
                name: "id_type",
                table: "kyc_records");

            migrationBuilder.AddColumn<string>(
                name: "aadhaar_document_key_back",
                table: "kyc_records",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "aadhaar_document_key_front",
                table: "kyc_records",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "aadhaar_number",
                table: "kyc_records",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pan_document_key_back",
                table: "kyc_records",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pan_document_key_front",
                table: "kyc_records",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pan_number",
                table: "kyc_records",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "aadhaar_document_key_back",
                table: "kyc_records");

            migrationBuilder.DropColumn(
                name: "aadhaar_document_key_front",
                table: "kyc_records");

            migrationBuilder.DropColumn(
                name: "aadhaar_number",
                table: "kyc_records");

            migrationBuilder.DropColumn(
                name: "pan_document_key_back",
                table: "kyc_records");

            migrationBuilder.DropColumn(
                name: "pan_document_key_front",
                table: "kyc_records");

            migrationBuilder.DropColumn(
                name: "pan_number",
                table: "kyc_records");

            migrationBuilder.AddColumn<string>(
                name: "id_number",
                table: "kyc_records",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "id_type",
                table: "kyc_records",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.InsertData(
                table: "kyc_records",
                columns: new[] { "id", "created_at", "id_number", "id_type", "kyc_status", "rejection_reason", "reviewed_at", "reviewed_by_id", "updated_at", "user_id" },
                values: new object[] { new Guid("33333333-3333-3333-3333-333333333333"), new DateTimeOffset(new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "999988887777", "Aadhaar", "Approved", null, null, null, null, new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.CreateIndex(
                name: "uq_kyc_records_id_type_number",
                table: "kyc_records",
                columns: new[] { "id_type", "id_number" },
                unique: true);
        }
    }
}
