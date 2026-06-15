using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedHealthProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO insurance_products (id, allows_family_floater, created_at, created_by_id, description, domain, is_active, max_age, max_family_members, max_sum_assured, max_tenure_years, min_age, min_sum_assured, min_tenure_years, product_name, uin, updated_at, waiting_period_days)
                VALUES ('70000000-0000-0000-0000-000000000001', true, '2026-06-07 00:00:00+00', null, 'Comprehensive health insurance with cashless hospitalisation across 5000+ network hospitals', 'HEALTH', true, 65, 6, 5000000.00, 10, 18, 100000.00, 1, 'SpeedCare Platinum Health', 'UIN-HC-DEMO-2026-SEED', null, 30)
                ON CONFLICT (id) DO NOTHING;
            ");

            migrationBuilder.Sql(@"
                INSERT INTO premium_rate_tables (id, age_max, age_min, annual_premium, created_at, product_id, sum_assured_max, sum_assured_min)
                VALUES
                    ('70000000-0000-0000-0000-000000000011', 30, 18, 8000.00, '2026-06-07 00:00:00+00', '70000000-0000-0000-0000-000000000001', 500000.00, 100000.00),
                    ('70000000-0000-0000-0000-000000000012', 45, 31, 12000.00, '2026-06-07 00:00:00+00', '70000000-0000-0000-0000-000000000001', 500000.00, 100000.00),
                    ('70000000-0000-0000-0000-000000000013', 65, 46, 18000.00, '2026-06-07 00:00:00+00', '70000000-0000-0000-0000-000000000001', 500000.00, 100000.00)
                ON CONFLICT (id) DO NOTHING;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("70000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("70000000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("70000000-0000-0000-0000-000000000013"));

            migrationBuilder.DeleteData(
                table: "insurance_products",
                keyColumn: "id",
                keyValue: new Guid("70000000-0000-0000-0000-000000000001"));
        }
    }
}
