using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceDemoProductCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "insurance_products",
                keyColumn: "id",
                keyValue: new Guid("10000000-1111-1111-1111-111111111111"));

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

            migrationBuilder.InsertData(
                table: "insurance_products",
                columns: new[] { "id", "allows_family_floater", "coverage_options_json", "created_at", "created_by_id", "description", "domain", "is_active", "is_available_for_sale", "max_age", "max_family_members", "max_sum_assured", "max_tenure_years", "min_age", "min_sum_assured", "min_tenure_years", "motor_vehicle_type", "product_name", "sum_assured_increment", "uin", "updated_at", "waiting_period_days" },
                values: new object[,]
                {
                    { new Guid("71000000-0000-0000-0000-000000000001"), true, "[300000,500000,1000000,1500000]", new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Fictional family health cover for catalog, quotation, and policy-Q&A testing.", "Health", true, true, 60, 6, 1500000m, 1, 18, 300000m, 1, null, "CareNest Family Shield", null, "UIN-HC-DEMO-2026-01", null, 30 },
                    { new Guid("71000000-0000-0000-0000-000000000002"), false, "[]", new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Fictional pure-protection term cover for catalog, quotation, and policy-Q&A testing.", "Life", true, true, 55, 1, 10000000m, 30, 18, 2500000m, 10, null, "Horizon Term Protect", 2500000m, "UIN-LI-DEMO-2026-01", null, 0 },
                    { new Guid("71000000-0000-0000-0000-000000000003"), false, "[]", new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Fictional private-car comprehensive cover for catalog, quotation, and claims-flow testing.", "Motor", true, true, 75, 1, 2000000m, 1, 18, 100000m, 1, "PrivateCar", "DriveSure Comprehensive", null, "UIN-MO-DEMO-2026-01", null, 0 }
                });

            migrationBuilder.InsertData(
                table: "document_requirements",
                columns: new[] { "id", "created_at", "description", "document_key", "domain", "entity_type", "is_active", "is_mandatory", "label", "product_id" },
                values: new object[,]
                {
                    { new Guid("71200000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Government-issued photo identity document.", "GOVERNMENT_ID", "Health", "Proposal", true, true, "Government photo ID", new Guid("71000000-0000-0000-0000-000000000001") },
                    { new Guid("71200000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Document confirming the proposer’s age.", "AGE_PROOF", "Health", "Proposal", true, true, "Age proof", new Guid("71000000-0000-0000-0000-000000000001") },
                    { new Guid("71200000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Completed health and medical declaration.", "MEDICAL_DECLARATION", "Health", "Proposal", true, true, "Medical declaration", new Guid("71000000-0000-0000-0000-000000000001") },
                    { new Guid("71200000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Required only when porting an existing health policy.", "PREVIOUS_POLICY", "Health", "Proposal", true, false, "Previous policy copy", new Guid("71000000-0000-0000-0000-000000000001") },
                    { new Guid("71200000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Government-issued photo identity document.", "GOVERNMENT_ID", "Life", "Proposal", true, true, "Government photo ID", new Guid("71000000-0000-0000-0000-000000000002") },
                    { new Guid("71200000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "PAN or other permitted tax identity document.", "PAN", "Life", "Proposal", true, true, "PAN or tax ID", new Guid("71000000-0000-0000-0000-000000000002") },
                    { new Guid("71200000-0000-0000-0000-000000000007"), new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Required for requested cover above INR 50 lakh.", "INCOME_PROOF", "Life", "Proposal", true, false, "Income proof", new Guid("71000000-0000-0000-0000-000000000002") },
                    { new Guid("71200000-0000-0000-0000-000000000008"), new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Required only when requested during underwriting.", "MEDICAL_TESTS", "Life", "Proposal", true, false, "Medical tests", new Guid("71000000-0000-0000-0000-000000000002") },
                    { new Guid("71200000-0000-0000-0000-000000000009"), new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Vehicle registration certificate (RC).", "REGISTRATION_CERTIFICATE", "Motor", "Proposal", true, true, "Registration certificate", new Guid("71000000-0000-0000-0000-000000000003") },
                    { new Guid("71200000-0000-0000-0000-000000000010"), new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Valid driving licence of the primary driver.", "DRIVING_LICENCE", "Motor", "Proposal", true, true, "Valid driving licence", new Guid("71000000-0000-0000-0000-000000000003") },
                    { new Guid("71200000-0000-0000-0000-000000000011"), new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Required where there is prior motor insurance coverage.", "PREVIOUS_POLICY", "Motor", "Proposal", true, false, "Existing policy copy", new Guid("71000000-0000-0000-0000-000000000003") },
                    { new Guid("71200000-0000-0000-0000-000000000012"), new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Required when inspection is requested.", "VEHICLE_PHOTOS", "Motor", "Proposal", true, false, "Vehicle photographs", new Guid("71000000-0000-0000-0000-000000000003") }
                });

            migrationBuilder.InsertData(
                table: "premium_rate_tables",
                columns: new[] { "id", "age_max", "age_min", "annual_premium", "created_at", "product_id", "sum_assured_max", "sum_assured_min" },
                values: new object[,]
                {
                    { new Guid("71100000-0000-0000-0000-000000000001"), 30, 18, 4800m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000001"), 300000m, 300000m },
                    { new Guid("71100000-0000-0000-0000-000000000002"), 30, 18, 6800m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000001"), 500000m, 500000m },
                    { new Guid("71100000-0000-0000-0000-000000000003"), 30, 18, 9900m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000001"), 1000000m, 1000000m },
                    { new Guid("71100000-0000-0000-0000-000000000004"), 30, 18, 13400m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000001"), 1500000m, 1500000m },
                    { new Guid("71100000-0000-0000-0000-000000000005"), 40, 31, 5900m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000001"), 300000m, 300000m },
                    { new Guid("71100000-0000-0000-0000-000000000006"), 40, 31, 8300m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000001"), 500000m, 500000m },
                    { new Guid("71100000-0000-0000-0000-000000000007"), 40, 31, 12100m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000001"), 1000000m, 1000000m },
                    { new Guid("71100000-0000-0000-0000-000000000008"), 40, 31, 16700m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000001"), 1500000m, 1500000m },
                    { new Guid("71100000-0000-0000-0000-000000000009"), 50, 41, 8300m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000001"), 300000m, 300000m },
                    { new Guid("71100000-0000-0000-0000-000000000010"), 50, 41, 11900m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000001"), 500000m, 500000m },
                    { new Guid("71100000-0000-0000-0000-000000000011"), 50, 41, 17600m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000001"), 1000000m, 1000000m },
                    { new Guid("71100000-0000-0000-0000-000000000012"), 50, 41, 24300m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000001"), 1500000m, 1500000m },
                    { new Guid("71100000-0000-0000-0000-000000000013"), 60, 51, 12500m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000001"), 300000m, 300000m },
                    { new Guid("71100000-0000-0000-0000-000000000014"), 60, 51, 17800m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000001"), 500000m, 500000m },
                    { new Guid("71100000-0000-0000-0000-000000000015"), 60, 51, 26400m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000001"), 1000000m, 1000000m },
                    { new Guid("71100000-0000-0000-0000-000000000016"), 60, 51, 36900m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000001"), 1500000m, 1500000m },
                    { new Guid("71100000-0000-0000-0000-000000000017"), 30, 18, 3200m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000002"), 2500000m, 2500000m },
                    { new Guid("71100000-0000-0000-0000-000000000018"), 30, 18, 5800m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000002"), 5000000m, 5000000m },
                    { new Guid("71100000-0000-0000-0000-000000000019"), 30, 18, 8400m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000002"), 7500000m, 7500000m },
                    { new Guid("71100000-0000-0000-0000-000000000020"), 30, 18, 10900m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000002"), 10000000m, 10000000m },
                    { new Guid("71100000-0000-0000-0000-000000000021"), 40, 31, 4700m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000002"), 2500000m, 2500000m },
                    { new Guid("71100000-0000-0000-0000-000000000022"), 40, 31, 8700m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000002"), 5000000m, 5000000m },
                    { new Guid("71100000-0000-0000-0000-000000000023"), 40, 31, 12500m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000002"), 7500000m, 7500000m },
                    { new Guid("71100000-0000-0000-0000-000000000024"), 40, 31, 16200m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000002"), 10000000m, 10000000m },
                    { new Guid("71100000-0000-0000-0000-000000000025"), 50, 41, 8900m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000002"), 2500000m, 2500000m },
                    { new Guid("71100000-0000-0000-0000-000000000026"), 50, 41, 16600m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000002"), 5000000m, 5000000m },
                    { new Guid("71100000-0000-0000-0000-000000000027"), 50, 41, 23900m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000002"), 7500000m, 7500000m },
                    { new Guid("71100000-0000-0000-0000-000000000028"), 50, 41, 31200m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000002"), 10000000m, 10000000m },
                    { new Guid("71100000-0000-0000-0000-000000000029"), 55, 51, 14800m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000002"), 2500000m, 2500000m },
                    { new Guid("71100000-0000-0000-0000-000000000030"), 55, 51, 27700m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000002"), 5000000m, 5000000m },
                    { new Guid("71100000-0000-0000-0000-000000000031"), 55, 51, 39800m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000002"), 7500000m, 7500000m },
                    { new Guid("71100000-0000-0000-0000-000000000032"), 55, 51, 52100m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000002"), 10000000m, 10000000m },
                    { new Guid("71100000-0000-0000-0000-000000000033"), 75, 18, 7200m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000003"), 500000m, 100000m },
                    { new Guid("71100000-0000-0000-0000-000000000034"), 75, 18, 10900m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000003"), 1000000m, 500001m },
                    { new Guid("71100000-0000-0000-0000-000000000035"), 75, 18, 15700m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000003"), 1500000m, 1000001m },
                    { new Guid("71100000-0000-0000-0000-000000000036"), 75, 18, 22600m, new DateTimeOffset(new DateTime(2026, 7, 19, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("71000000-0000-0000-0000-000000000003"), 2000000m, 1500001m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "document_requirements",
                keyColumn: "id",
                keyValue: new Guid("71200000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "document_requirements",
                keyColumn: "id",
                keyValue: new Guid("71200000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "document_requirements",
                keyColumn: "id",
                keyValue: new Guid("71200000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "document_requirements",
                keyColumn: "id",
                keyValue: new Guid("71200000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "document_requirements",
                keyColumn: "id",
                keyValue: new Guid("71200000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "document_requirements",
                keyColumn: "id",
                keyValue: new Guid("71200000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "document_requirements",
                keyColumn: "id",
                keyValue: new Guid("71200000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "document_requirements",
                keyColumn: "id",
                keyValue: new Guid("71200000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "document_requirements",
                keyColumn: "id",
                keyValue: new Guid("71200000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "document_requirements",
                keyColumn: "id",
                keyValue: new Guid("71200000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "document_requirements",
                keyColumn: "id",
                keyValue: new Guid("71200000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "document_requirements",
                keyColumn: "id",
                keyValue: new Guid("71200000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000013"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000014"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000015"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000016"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000017"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000018"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000019"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000020"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000021"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000022"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000023"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000024"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000025"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000026"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000027"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000028"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000029"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000030"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000031"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000032"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000033"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000034"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000035"));

            migrationBuilder.DeleteData(
                table: "premium_rate_tables",
                keyColumn: "id",
                keyValue: new Guid("71100000-0000-0000-0000-000000000036"));

            migrationBuilder.DeleteData(
                table: "insurance_products",
                keyColumn: "id",
                keyValue: new Guid("71000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "insurance_products",
                keyColumn: "id",
                keyValue: new Guid("71000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "insurance_products",
                keyColumn: "id",
                keyValue: new Guid("71000000-0000-0000-0000-000000000003"));

            migrationBuilder.InsertData(
                table: "insurance_products",
                columns: new[] { "id", "allows_family_floater", "coverage_options_json", "created_at", "created_by_id", "description", "domain", "is_active", "is_available_for_sale", "max_age", "max_family_members", "max_sum_assured", "max_tenure_years", "min_age", "min_sum_assured", "min_tenure_years", "motor_vehicle_type", "product_name", "sum_assured_increment", "uin", "updated_at", "waiting_period_days" },
                values: new object[,]
                {
                    { new Guid("10000000-1111-1111-1111-111111111111"), false, "[]", new DateTimeOffset(new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Basic term life insurance", "LIFE", true, true, 60, 1, 5000000m, 30, 18, 500000m, 5, null, "Term Life Basic", 100000m, "UIN123", null, 0 },
                    { new Guid("70000000-0000-0000-0000-000000000001"), true, "[300000,500000,1000000,1500000]", new DateTimeOffset(new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Comprehensive health insurance with cashless hospitalisation across 5000+ network hospitals", "HEALTH", true, true, 65, 6, 1500000m, 10, 18, 300000m, 1, null, "SpeedCare Platinum Health", null, "UIN-HC-DEMO-2026", null, 30 }
                });

            migrationBuilder.InsertData(
                table: "premium_rate_tables",
                columns: new[] { "id", "age_max", "age_min", "annual_premium", "created_at", "product_id", "sum_assured_max", "sum_assured_min" },
                values: new object[,]
                {
                    { new Guid("70000000-0000-0000-0000-000000000011"), 30, 18, 8000m, new DateTimeOffset(new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("70000000-0000-0000-0000-000000000001"), 500000m, 100000m },
                    { new Guid("70000000-0000-0000-0000-000000000012"), 45, 31, 12000m, new DateTimeOffset(new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("70000000-0000-0000-0000-000000000001"), 500000m, 100000m },
                    { new Guid("70000000-0000-0000-0000-000000000013"), 65, 46, 18000m, new DateTimeOffset(new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("70000000-0000-0000-0000-000000000001"), 500000m, 100000m }
                });
        }
    }
}
