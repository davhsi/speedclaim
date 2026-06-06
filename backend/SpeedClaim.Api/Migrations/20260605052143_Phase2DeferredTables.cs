using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class Phase2DeferredTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "agents",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    old_values = table.Column<string>(type: "jsonb", nullable: true),
                    new_values = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "claim_document_checklists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_id = table.Column<Guid>(type: "uuid", nullable: false),
                    domain = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    document_type_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_received = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claim_document_checklist", x => x.id);
                    table.ForeignKey(
                        name: "FK_checklist_claim",
                        column: x => x.claim_id,
                        principalTable: "claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "claim_health_details",
                columns: table => new
                {
                    claim_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hospital_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    diagnosis = table.Column<string>(type: "text", nullable: false),
                    treating_doctor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    admission_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    discharge_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_cashless = table.Column<bool>(type: "boolean", nullable: false),
                    insured_member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claim_health_details", x => x.claim_id);
                    table.ForeignKey(
                        name: "FK_claim_health_details_claims_claim_id",
                        column: x => x.claim_id,
                        principalTable: "claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_claim_health_details_policy_insured_members_insured_member_id",
                        column: x => x.insured_member_id,
                        principalTable: "policy_insured_members",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "claim_life_details",
                columns: table => new
                {
                    claim_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cause_of_death = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    place_of_death = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    death_certificate_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    certifying_doctor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    claimant_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    claimant_relation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claim_life_details", x => x.claim_id);
                    table.ForeignKey(
                        name: "FK_claim_life_details_claims_claim_id",
                        column: x => x.claim_id,
                        principalTable: "claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "claim_vehicle_details",
                columns: table => new
                {
                    claim_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accident_location = table.Column<string>(type: "text", nullable: false),
                    fir_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    repair_estimate = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    is_total_loss = table.Column<bool>(type: "boolean", nullable: false),
                    surveyor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claim_vehicle_details", x => x.claim_id);
                    table.ForeignKey(
                        name: "FK_claim_vehicle_details_claims_claim_id",
                        column: x => x.claim_id,
                        principalTable: "claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_status_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    old_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    new_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    changed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    remarks = table.Column<string>(type: "text", nullable: true),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_status_history_payment_transactions_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payment_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_payment_status_history_users_changed_by_id",
                        column: x => x.changed_by_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "premium_schedules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    installment_number = table.Column<int>(type: "integer", nullable: false),
                    amount_due = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_premium_schedule", x => x.id);
                    table.ForeignKey(
                        name: "FK_premium_schedule_payment_transactions_payment_id",
                        column: x => x.payment_id,
                        principalTable: "payment_transactions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_premium_schedule_policies_policy_id",
                        column: x => x.policy_id,
                        principalTable: "policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_consents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consent_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_granted = table.Column<bool>(type: "boolean", nullable: false),
                    consent_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    withdrawn_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_consents", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_consents_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_audit_logs_composite",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_claim_document_checklist_claim_id_document_type_code",
                table: "claim_document_checklists",
                columns: new[] { "claim_id", "document_type_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_claim_health_details_insured_member_id",
                table: "claim_health_details",
                column: "insured_member_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_status_histories_changed_by_id",
                table: "payment_status_histories",
                column: "changed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_status_histories_payment_id",
                table: "payment_status_histories",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "idx_premium_schedule_due",
                table: "premium_schedules",
                columns: new[] { "due_date", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_premium_schedules_payment_id",
                table: "premium_schedules",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_premium_schedules_policy_id",
                table: "premium_schedules",
                column: "policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_consents_user_id",
                table: "user_consents",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "claim_document_checklists");

            migrationBuilder.DropTable(
                name: "claim_health_details");

            migrationBuilder.DropTable(
                name: "claim_life_details");

            migrationBuilder.DropTable(
                name: "claim_vehicle_details");

            migrationBuilder.DropTable(
                name: "payment_status_histories");

            migrationBuilder.DropTable(
                name: "premium_schedules");

            migrationBuilder.DropTable(
                name: "user_consents");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "agents");
        }
    }
}
