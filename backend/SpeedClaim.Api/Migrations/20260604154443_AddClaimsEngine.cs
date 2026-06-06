using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimsEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "claims",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submitted_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_adjuster_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    claimed_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    approved_amount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    incident_description = table.Column<string>(type: "text", nullable: false),
                    priority = table.Column<short>(type: "smallint", nullable: false),
                    domain = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    incident_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_automated_processed = table.Column<bool>(type: "boolean", nullable: false),
                    risk_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claims", x => x.id);
                    table.CheckConstraint("CK_claims_priority", "priority IN (1, 2, 3)");
                    table.CheckConstraint("CK_claims_status", "status IN ('SUBMITTED', 'UNDER_REVIEW', 'ESCALATED', 'APPROVED', 'REJECTED', 'SETTLED', 'CLOSED')");
                    table.ForeignKey(
                        name: "FK_claims_policies_policy_id",
                        column: x => x.policy_id,
                        principalTable: "policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_claims_users_assigned_adjuster_id",
                        column: x => x.assigned_adjuster_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_claims_users_submitted_by_id",
                        column: x => x.submitted_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "claim_workflows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    to_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    remarks = table.Column<string>(type: "text", nullable: true),
                    transitioned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claim_workflow", x => x.id);
                    table.ForeignKey(
                        name: "FK_claim_workflow_claims_claim_id",
                        column: x => x.claim_id,
                        principalTable: "claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_claim_workflow_users_actor_id",
                        column: x => x.actor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_documents_claim_id",
                table: "documents",
                column: "claim_id");

            migrationBuilder.CreateIndex(
                name: "IX_documents_policy_id",
                table: "documents",
                column: "policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_claim_workflows_actor_id",
                table: "claim_workflows",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_claim_workflows_claim_id",
                table: "claim_workflows",
                column: "claim_id");

            migrationBuilder.CreateIndex(
                name: "IX_claims_assigned_adjuster_id",
                table: "claims",
                column: "assigned_adjuster_id");

            migrationBuilder.CreateIndex(
                name: "IX_claims_policy_id",
                table: "claims",
                column: "policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_claims_submitted_by_id",
                table: "claims",
                column: "submitted_by_id");

            migrationBuilder.CreateIndex(
                name: "UQ_claim_id_domain",
                table: "claims",
                columns: new[] { "id", "domain" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_claims_claim_number",
                table: "claims",
                column: "claim_number",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_documents_claims_claim_id",
                table: "documents",
                column: "claim_id",
                principalTable: "claims",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_documents_policies_policy_id",
                table: "documents",
                column: "policy_id",
                principalTable: "policies",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_documents_claims_claim_id",
                table: "documents");

            migrationBuilder.DropForeignKey(
                name: "FK_documents_policies_policy_id",
                table: "documents");

            migrationBuilder.DropTable(
                name: "claim_workflows");

            migrationBuilder.DropTable(
                name: "claims");

            migrationBuilder.DropIndex(
                name: "IX_documents_claim_id",
                table: "documents");

            migrationBuilder.DropIndex(
                name: "IX_documents_policy_id",
                table: "documents");
        }
    }
}
