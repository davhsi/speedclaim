using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class AlignSpecDataTypesTrueFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_policies_users_issued_by",
                table: "policies");

            migrationBuilder.DropIndex(
                name: "IX_policies_issued_by_id",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "issued_by_id",
                table: "policies");

            migrationBuilder.RenameColumn(
                name: "is_revoked",
                table: "user_tokens",
                newName: "is_used");

            migrationBuilder.RenameColumn(
                name: "amount_due",
                table: "premium_schedules",
                newName: "amount");

            migrationBuilder.RenameColumn(
                name: "reason",
                table: "claim_status_histories",
                newName: "notes");

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "salutation",
                value: "Mr");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "is_used",
                table: "user_tokens",
                newName: "is_revoked");

            migrationBuilder.RenameColumn(
                name: "amount",
                table: "premium_schedules",
                newName: "amount_due");

            migrationBuilder.RenameColumn(
                name: "notes",
                table: "claim_status_histories",
                newName: "reason");

            migrationBuilder.AddColumn<Guid>(
                name: "issued_by_id",
                table: "policies",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "salutation",
                value: "Mr.");

            migrationBuilder.CreateIndex(
                name: "IX_policies_issued_by_id",
                table: "policies",
                column: "issued_by_id");

            migrationBuilder.AddForeignKey(
                name: "FK_policies_users_issued_by",
                table: "policies",
                column: "issued_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
