using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedDomainAwareCoverage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "insurance_products",
                keyColumn: "id",
                keyValue: new Guid("10000000-1111-1111-1111-111111111111"),
                columns: new[] { "min_sum_assured", "sum_assured_increment" },
                values: new object[] { 500000m, 100000m });

            migrationBuilder.UpdateData(
                table: "insurance_products",
                keyColumn: "id",
                keyValue: new Guid("70000000-0000-0000-0000-000000000001"),
                columns: new[] { "coverage_options_json", "max_sum_assured", "min_sum_assured" },
                values: new object[] { "[300000,500000,1000000,1500000]", 1500000m, 300000m });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "insurance_products",
                keyColumn: "id",
                keyValue: new Guid("10000000-1111-1111-1111-111111111111"),
                columns: new[] { "min_sum_assured", "sum_assured_increment" },
                values: new object[] { 100000m, null });

            migrationBuilder.UpdateData(
                table: "insurance_products",
                keyColumn: "id",
                keyValue: new Guid("70000000-0000-0000-0000-000000000001"),
                columns: new[] { "coverage_options_json", "max_sum_assured", "min_sum_assured" },
                values: new object[] { "[]", 5000000m, 100000m });
        }
    }
}
