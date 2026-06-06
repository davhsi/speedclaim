using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentRejectionReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "documents",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "reviewed_by_id",
                table: "documents",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_documents_reviewed_by_id",
                table: "documents",
                column: "reviewed_by_id");

            migrationBuilder.AddForeignKey(
                name: "FK_documents_users_reviewed_by_id",
                table: "documents",
                column: "reviewed_by_id",
                principalTable: "users",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_documents_users_reviewed_by_id",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_documents_reviewed_by_id",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "rejection_reason",
                table: "documents");

            migrationBuilder.DropColumn(
                name: "reviewed_by_id",
                table: "documents");
        }
    }
}
