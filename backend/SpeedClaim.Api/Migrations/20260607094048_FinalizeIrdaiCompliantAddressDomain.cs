using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class FinalizeIrdaiCompliantAddressDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_addresses_address_id",
                table: "users");

            migrationBuilder.RenameColumn(
                name: "address_id",
                table: "users",
                newName: "permanent_address_id");

            migrationBuilder.RenameIndex(
                name: "IX_users_address_id",
                table: "users",
                newName: "IX_users_permanent_address_id");

            migrationBuilder.RenameColumn(
                name: "street",
                table: "addresses",
                newName: "line1");

            migrationBuilder.AddColumn<Guid>(
                name: "current_address_id",
                table: "users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "line2",
                table: "addresses",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_current_address_id",
                table: "users",
                column: "current_address_id");

            migrationBuilder.AddForeignKey(
                name: "FK_users_addresses_current_address_id",
                table: "users",
                column: "current_address_id",
                principalTable: "addresses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_users_addresses_permanent_address_id",
                table: "users",
                column: "permanent_address_id",
                principalTable: "addresses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_addresses_current_address_id",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "FK_users_addresses_permanent_address_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_current_address_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "current_address_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "line2",
                table: "addresses");

            migrationBuilder.RenameColumn(
                name: "permanent_address_id",
                table: "users",
                newName: "address_id");

            migrationBuilder.RenameIndex(
                name: "IX_users_permanent_address_id",
                table: "users",
                newName: "IX_users_address_id");

            migrationBuilder.RenameColumn(
                name: "line1",
                table: "addresses",
                newName: "street");

            migrationBuilder.AddForeignKey(
                name: "FK_users_addresses_address_id",
                table: "users",
                column: "address_id",
                principalTable: "addresses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
