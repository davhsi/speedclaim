using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class ApplyV3Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_claims_customer_members_claimant_member_id",
                table: "claims");

            migrationBuilder.DropForeignKey(
                name: "FK_claims_users_surveyor_id",
                table: "claims");

            migrationBuilder.DropForeignKey(
                name: "FK_nominees_policies_policy_id",
                table: "nominees");

            migrationBuilder.DropForeignKey(
                name: "FK_premium_payments_policies_policy_id",
                table: "premium_payments");

            migrationBuilder.DropForeignKey(
                name: "FK_premium_payments_schedules",
                table: "premium_payments");

            migrationBuilder.DropForeignKey(
                name: "FK_premium_schedules_policies_policy_id",
                table: "premium_schedules");

            migrationBuilder.DropTable(
                name: "health_policy_details");

            migrationBuilder.DropTable(
                name: "life_policy_details");

            migrationBuilder.DropTable(
                name: "motor_policy_details");

            migrationBuilder.AlterColumn<Guid>(
                name: "policy_id",
                table: "premium_schedules",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                table: "premium_schedules",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "proposal_id",
                table: "premium_schedules",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "premium_schedules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "schedule_id",
                table: "premium_payments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "policy_id",
                table: "premium_payments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "proposal_id",
                table: "premium_payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "policy_id",
                table: "nominees",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "proposal_id",
                table: "nominees",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "claimant_member_id",
                table: "claims",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateTable(
                name: "health_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    pre_existing_conditions = table.Column<string>(type: "text", nullable: false),
                    network_hospital_coverage = table.Column<string>(type: "text", nullable: false),
                    tpa_name = table.Column<string>(type: "text", nullable: false),
                    room_rent_limit = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    maternity_covered = table.Column<bool>(type: "boolean", nullable: false),
                    copay_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_health_details", x => x.id);
                    table.ForeignKey(
                        name: "FK_health_details_policies",
                        column: x => x.policy_id,
                        principalTable: "policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_health_details_proposals",
                        column: x => x.proposal_id,
                        principalTable: "proposals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "life_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    policy_subtype = table.Column<string>(type: "text", nullable: false),
                    maturity_benefit = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    death_benefit = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    surrender_value_applicable = table.Column<bool>(type: "boolean", nullable: false),
                    loan_eligible = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_life_details", x => x.id);
                    table.ForeignKey(
                        name: "FK_life_details_policies",
                        column: x => x.policy_id,
                        principalTable: "policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_life_details_proposals",
                        column: x => x.proposal_id,
                        principalTable: "proposals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "motor_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    vehicle_number = table.Column<string>(type: "text", nullable: false),
                    vehicle_make = table.Column<string>(type: "text", nullable: false),
                    vehicle_model = table.Column<string>(type: "text", nullable: false),
                    manufacture_year = table.Column<int>(type: "integer", nullable: false),
                    vehicle_type = table.Column<string>(type: "text", nullable: false),
                    idv = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    engine_number = table.Column<string>(type: "text", nullable: false),
                    chassis_number = table.Column<string>(type: "text", nullable: false),
                    cover_type = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_motor_details", x => x.id);
                    table.ForeignKey(
                        name: "FK_motor_details_policies",
                        column: x => x.policy_id,
                        principalTable: "policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_motor_details_proposals",
                        column: x => x.proposal_id,
                        principalTable: "proposals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "surveyors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    surveyor_type = table.Column<string>(type: "text", nullable: false),
                    license_number = table.Column<string>(type: "text", nullable: true),
                    license_expiry = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    specialization = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_surveyors", x => x.id);
                    table.ForeignKey(
                        name: "FK_surveyors_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_type = table.Column<string>(type: "text", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_premium_schedules_proposal_id",
                table: "premium_schedules",
                column: "proposal_id");

            migrationBuilder.CreateIndex(
                name: "IX_premium_payments_proposal_id",
                table: "premium_payments",
                column: "proposal_id");

            migrationBuilder.CreateIndex(
                name: "IX_nominees_proposal_id",
                table: "nominees",
                column: "proposal_id");

            migrationBuilder.CreateIndex(
                name: "IX_health_details_policy_id",
                table: "health_details",
                column: "policy_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_health_details_proposal_id",
                table: "health_details",
                column: "proposal_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_life_details_policy_id",
                table: "life_details",
                column: "policy_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_life_details_proposal_id",
                table: "life_details",
                column: "proposal_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_motor_details_policy_id",
                table: "motor_details",
                column: "policy_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_motor_details_proposal_id",
                table: "motor_details",
                column: "proposal_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_surveyors_user_id",
                table: "surveyors",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_tokens_user_id",
                table: "user_tokens",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_claims_customer_members_claimant_member_id",
                table: "claims",
                column: "claimant_member_id",
                principalTable: "customer_members",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_claims_surveyors",
                table: "claims",
                column: "surveyor_id",
                principalTable: "surveyors",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_nominees_policies",
                table: "nominees",
                column: "policy_id",
                principalTable: "policies",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_nominees_proposals",
                table: "nominees",
                column: "proposal_id",
                principalTable: "proposals",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_premium_payments_policies",
                table: "premium_payments",
                column: "policy_id",
                principalTable: "policies",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_premium_payments_proposals",
                table: "premium_payments",
                column: "proposal_id",
                principalTable: "proposals",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_premium_payments_schedules",
                table: "premium_payments",
                column: "schedule_id",
                principalTable: "premium_schedules",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_premium_schedules_policies",
                table: "premium_schedules",
                column: "policy_id",
                principalTable: "policies",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_premium_schedules_proposals",
                table: "premium_schedules",
                column: "proposal_id",
                principalTable: "proposals",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_claims_customer_members_claimant_member_id",
                table: "claims");

            migrationBuilder.DropForeignKey(
                name: "FK_claims_surveyors",
                table: "claims");

            migrationBuilder.DropForeignKey(
                name: "FK_nominees_policies",
                table: "nominees");

            migrationBuilder.DropForeignKey(
                name: "FK_nominees_proposals",
                table: "nominees");

            migrationBuilder.DropForeignKey(
                name: "FK_premium_payments_policies",
                table: "premium_payments");

            migrationBuilder.DropForeignKey(
                name: "FK_premium_payments_proposals",
                table: "premium_payments");

            migrationBuilder.DropForeignKey(
                name: "FK_premium_payments_schedules",
                table: "premium_payments");

            migrationBuilder.DropForeignKey(
                name: "FK_premium_schedules_policies",
                table: "premium_schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_premium_schedules_proposals",
                table: "premium_schedules");

            migrationBuilder.DropTable(
                name: "health_details");

            migrationBuilder.DropTable(
                name: "life_details");

            migrationBuilder.DropTable(
                name: "motor_details");

            migrationBuilder.DropTable(
                name: "surveyors");

            migrationBuilder.DropTable(
                name: "user_tokens");

            migrationBuilder.DropIndex(
                name: "IX_premium_schedules_proposal_id",
                table: "premium_schedules");

            migrationBuilder.DropIndex(
                name: "IX_premium_payments_proposal_id",
                table: "premium_payments");

            migrationBuilder.DropIndex(
                name: "IX_nominees_proposal_id",
                table: "nominees");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "premium_schedules");

            migrationBuilder.DropColumn(
                name: "proposal_id",
                table: "premium_schedules");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "premium_schedules");

            migrationBuilder.DropColumn(
                name: "proposal_id",
                table: "premium_payments");

            migrationBuilder.DropColumn(
                name: "proposal_id",
                table: "nominees");

            migrationBuilder.AlterColumn<Guid>(
                name: "policy_id",
                table: "premium_schedules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "schedule_id",
                table: "premium_payments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "policy_id",
                table: "premium_payments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "policy_id",
                table: "nominees",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "claimant_member_id",
                table: "claims",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "health_policy_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    copay_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    maternity_covered = table.Column<bool>(type: "boolean", nullable: false),
                    network_hospital_coverage = table.Column<string>(type: "text", nullable: false),
                    pre_existing_conditions = table.Column<string>(type: "text", nullable: false),
                    room_rent_limit = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    tpa_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_health_policy_details", x => x.id);
                    table.ForeignKey(
                        name: "FK_health_policy_details_policies",
                        column: x => x.policy_id,
                        principalTable: "policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "life_policy_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    death_benefit = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    loan_eligible = table.Column<bool>(type: "boolean", nullable: false),
                    maturity_benefit = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    policy_subtype = table.Column<string>(type: "text", nullable: false),
                    surrender_value_applicable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_life_policy_details", x => x.id);
                    table.ForeignKey(
                        name: "FK_life_policy_details_policies",
                        column: x => x.policy_id,
                        principalTable: "policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "motor_policy_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    chassis_number = table.Column<string>(type: "text", nullable: false),
                    cover_type = table.Column<string>(type: "text", nullable: false),
                    engine_number = table.Column<string>(type: "text", nullable: false),
                    idv = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    manufacture_year = table.Column<int>(type: "integer", nullable: false),
                    vehicle_make = table.Column<string>(type: "text", nullable: false),
                    vehicle_model = table.Column<string>(type: "text", nullable: false),
                    vehicle_number = table.Column<string>(type: "text", nullable: false),
                    vehicle_type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_motor_policy_details", x => x.id);
                    table.ForeignKey(
                        name: "FK_motor_policy_details_policies",
                        column: x => x.policy_id,
                        principalTable: "policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_health_policy_details_policy_id",
                table: "health_policy_details",
                column: "policy_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_life_policy_details_policy_id",
                table: "life_policy_details",
                column: "policy_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_motor_policy_details_policy_id",
                table: "motor_policy_details",
                column: "policy_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_claims_customer_members_claimant_member_id",
                table: "claims",
                column: "claimant_member_id",
                principalTable: "customer_members",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_claims_users_surveyor_id",
                table: "claims",
                column: "surveyor_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_nominees_policies_policy_id",
                table: "nominees",
                column: "policy_id",
                principalTable: "policies",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_premium_payments_policies_policy_id",
                table: "premium_payments",
                column: "policy_id",
                principalTable: "policies",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_premium_payments_schedules",
                table: "premium_payments",
                column: "schedule_id",
                principalTable: "premium_schedules",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_premium_schedules_policies_policy_id",
                table: "premium_schedules",
                column: "policy_id",
                principalTable: "policies",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
