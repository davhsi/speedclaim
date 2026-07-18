using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class DomainAwareCoverage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "coverage_options_json",
                table: "insurance_products",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<decimal>(
                name: "sum_assured_increment",
                table: "insurance_products",
                type: "numeric(15,2)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "insurance_products",
                keyColumn: "id",
                keyValue: new Guid("10000000-1111-1111-1111-111111111111"),
                columns: new[] { "coverage_options_json", "sum_assured_increment" },
                values: new object[] { "[]", null });

            migrationBuilder.UpdateData(
                table: "insurance_products",
                keyColumn: "id",
                keyValue: new Guid("70000000-0000-0000-0000-000000000001"),
                columns: new[] { "coverage_options_json", "sum_assured_increment" },
                values: new object[] { "[]", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "coverage_options_json",
                table: "insurance_products");

            migrationBuilder.DropColumn(
                name: "sum_assured_increment",
                table: "insurance_products");
        }
    }
}
