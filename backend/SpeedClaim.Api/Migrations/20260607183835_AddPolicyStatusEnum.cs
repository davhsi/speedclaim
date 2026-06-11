using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyStatusEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_policies_status",
                table: "policies");

            migrationBuilder.AddCheckConstraint(
                name: "CK_policies_status",
                table: "policies",
                sql: "status IN ('Pending', 'Active', 'Lapsed', 'Cancelled', 'Expired', 'Claimed')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_policies_status",
                table: "policies");

            migrationBuilder.AddCheckConstraint(
                name: "CK_policies_status",
                table: "policies",
                sql: "status IN ('ACTIVE', 'LAPSED', 'CANCELLED', 'EXPIRED', 'CLAIMED')");
        }
    }
}
