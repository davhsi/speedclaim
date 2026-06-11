using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedInitialProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "insurance_products",
                columns: new[] { "id", "code", "description", "domain", "is_active", "max_coverage", "name" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "PRD-LIFE-001", "Basic term life insurance", "LIFE", true, 100000m, "Term Life Basic" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "PRD-HLTH-001", "Comprehensive health insurance", "HEALTH", true, 50000m, "Health Shield" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "PRD-AUTO-001", "Full coverage auto insurance", "AUTO", true, 30000m, "Auto Protect" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "insurance_products",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "insurance_products",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "insurance_products",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));
        }
    }
}
