using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "addresses",
                columns: new[] { "id", "city", "country", "line1", "line2", "postal_code", "state" },
                values: new object[] { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Admin City", "India", "123 Admin St", null, "123456", "Admin State" });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "aadhaar_number", "created_at", "current_address_id", "date_of_birth", "deleted_at", "email", "first_name", "gender", "is_active", "kyc_status", "last_name", "marital_status", "pan_number", "password_hash", "permanent_address_id", "phone", "profile_picture_url", "salutation", "timezone" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "999988887777", new DateTimeOffset(new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new DateTime(1995, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "davish.std@gmail.com", "Davish", "Male", true, "VERIFIED", "Dev", "Single", "ABCDE9999F", "$2a$11$O.SrE3dQ5wjiWGc20uH7HuIuFwnrCYiuROffc9k/nk6./69E2z2.S", new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "9998887776", "", "Mr.", "UTC" });

            migrationBuilder.InsertData(
                table: "user_roles",
                columns: new[] { "id", "approval_limit", "assigned_at", "domain", "revoked_at", "role_id", "user_id" },
                values: new object[] { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), null, new DateTimeOffset(new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AUTH", null, new Guid("33333333-3333-3333-3333-333333333333"), new Guid("11111111-1111-1111-1111-111111111111") });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "user_roles",
                keyColumn: "id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "addresses",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        }
    }
}
