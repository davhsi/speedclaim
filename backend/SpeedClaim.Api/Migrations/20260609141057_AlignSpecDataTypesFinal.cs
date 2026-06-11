using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class AlignSpecDataTypesFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_claim_status_histories_users_changed_by_id",
                table: "claim_status_histories");

            migrationBuilder.DropForeignKey(
                name: "FK_endorsements_users_requested_by_id",
                table: "endorsements");

            migrationBuilder.DropForeignKey(
                name: "FK_endorsements_users_reviewed_by_id",
                table: "endorsements");

            migrationBuilder.RenameColumn(
                name: "notes",
                table: "claim_status_histories",
                newName: "reason");

            migrationBuilder.AddColumn<Guid>(
                name: "issued_by_id",
                table: "policies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "changed_by_id",
                table: "claim_status_histories",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "IX_policies_issued_by_id",
                table: "policies",
                column: "issued_by_id");

            migrationBuilder.AddForeignKey(
                name: "FK_claim_status_histories_users_changed_by_id",
                table: "claim_status_histories",
                column: "changed_by_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_endorsements_users_requested_by",
                table: "endorsements",
                column: "requested_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_endorsements_users_reviewed_by",
                table: "endorsements",
                column: "reviewed_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_policies_users_issued_by",
                table: "policies",
                column: "issued_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_claim_status_histories_users_changed_by_id",
                table: "claim_status_histories");

            migrationBuilder.DropForeignKey(
                name: "FK_endorsements_users_requested_by",
                table: "endorsements");

            migrationBuilder.DropForeignKey(
                name: "FK_endorsements_users_reviewed_by",
                table: "endorsements");

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
                name: "reason",
                table: "claim_status_histories",
                newName: "notes");

            migrationBuilder.AlterColumn<Guid>(
                name: "changed_by_id",
                table: "claim_status_histories",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_claim_status_histories_users_changed_by_id",
                table: "claim_status_histories",
                column: "changed_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_endorsements_users_requested_by_id",
                table: "endorsements",
                column: "requested_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_endorsements_users_reviewed_by_id",
                table: "endorsements",
                column: "reviewed_by_id",
                principalTable: "users",
                principalColumn: "id");
        }
    }
}
