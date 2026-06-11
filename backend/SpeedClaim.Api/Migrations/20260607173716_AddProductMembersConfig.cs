using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProductMembersConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "allows_insured_members",
                table: "insurance_products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "max_insured_members",
                table: "insurance_products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "insurance_products",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "allows_insured_members", "max_insured_members" },
                values: new object[] { true, 1 });

            migrationBuilder.UpdateData(
                table: "insurance_products",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                columns: new[] { "allows_insured_members", "max_insured_members" },
                values: new object[] { true, 5 });

            migrationBuilder.UpdateData(
                table: "insurance_products",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "allows_insured_members", "max_insured_members" },
                values: new object[] { false, 0 });

            migrationBuilder.AddCheckConstraint(
                name: "CK_products_max_members",
                table: "insurance_products",
                sql: "max_insured_members >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_products_max_members",
                table: "insurance_products");

            migrationBuilder.DropColumn(
                name: "allows_insured_members",
                table: "insurance_products");

            migrationBuilder.DropColumn(
                name: "max_insured_members",
                table: "insurance_products");
        }
    }
}
