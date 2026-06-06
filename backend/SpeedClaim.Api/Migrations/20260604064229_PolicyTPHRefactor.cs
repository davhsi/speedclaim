using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class PolicyTPHRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "policy_health_details");

            migrationBuilder.DropTable(
                name: "policy_life_details");

            migrationBuilder.DropTable(
                name: "policy_vehicle_details");

            migrationBuilder.AddColumn<bool>(
                name: "covers_dental",
                table: "policies",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "deductible",
                table: "policies",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "domain1",
                table: "policies",
                type: "character varying(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "has_accidental_rider",
                table: "policies",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "insured_declared_value",
                table: "policies",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_comprehensive",
                table: "policies",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "make",
                table: "policies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "manufacture_year",
                table: "policies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "model",
                table: "policies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "network_type",
                table: "policies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nominee_name",
                table: "policies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nominee_phone",
                table: "policies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "nominee_relation",
                table: "policies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "vehicle_number",
                table: "policies",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "covers_dental",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "deductible",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "domain1",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "has_accidental_rider",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "insured_declared_value",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "is_comprehensive",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "make",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "manufacture_year",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "model",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "network_type",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "nominee_name",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "nominee_phone",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "nominee_relation",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "vehicle_number",
                table: "policies");

            migrationBuilder.CreateTable(
                name: "policy_health_details",
                columns: table => new
                {
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    covers_dental = table.Column<bool>(type: "boolean", nullable: false),
                    deductible = table.Column<decimal>(type: "numeric", nullable: false),
                    network_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_policy_health_details", x => x.policy_id);
                    table.CheckConstraint("CK_health_network", "network_type IN ('TPA', 'CASHLESS', 'REIMBURSEMENT')");
                    table.ForeignKey(
                        name: "FK_policy_health_details_policies_policy_id",
                        column: x => x.policy_id,
                        principalTable: "policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "policy_life_details",
                columns: table => new
                {
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    has_accidental_rider = table.Column<bool>(type: "boolean", nullable: false),
                    nominee_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    nominee_phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    nominee_relation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_policy_life_details", x => x.policy_id);
                    table.ForeignKey(
                        name: "FK_policy_life_details_policies_policy_id",
                        column: x => x.policy_id,
                        principalTable: "policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "policy_vehicle_details",
                columns: table => new
                {
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    insured_declared_value = table.Column<decimal>(type: "numeric", nullable: false),
                    is_comprehensive = table.Column<bool>(type: "boolean", nullable: false),
                    make = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    manufacture_year = table.Column<int>(type: "integer", nullable: false),
                    model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    vehicle_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_policy_vehicle_details", x => x.policy_id);
                    table.ForeignKey(
                        name: "FK_policy_vehicle_details_policies_policy_id",
                        column: x => x.policy_id,
                        principalTable: "policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "uq_policy_vehicle_details_vehicle_number",
                table: "policy_vehicle_details",
                column: "vehicle_number",
                unique: true);
        }
    }
}
