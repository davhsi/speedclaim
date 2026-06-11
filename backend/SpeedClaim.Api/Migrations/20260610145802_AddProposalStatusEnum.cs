using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProposalStatusEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_agent_commissions_agents_agent_id",
                table: "agent_commissions");

            migrationBuilder.DropForeignKey(
                name: "FK_agent_commissions_policies_policy_id",
                table: "agent_commissions");

            migrationBuilder.DropForeignKey(
                name: "FK_agent_commissions_premium_payments_premium_payment_id",
                table: "agent_commissions");

            migrationBuilder.DropForeignKey(
                name: "FK_claim_status_histories_claims_claim_id",
                table: "claim_status_histories");

            migrationBuilder.DropForeignKey(
                name: "FK_claim_status_histories_users_changed_by_id",
                table: "claim_status_histories");

            migrationBuilder.DropForeignKey(
                name: "FK_claims_customer_members_claimant_member_id",
                table: "claims");

            migrationBuilder.DropForeignKey(
                name: "FK_claims_customers_customer_id",
                table: "claims");

            migrationBuilder.DropForeignKey(
                name: "FK_claims_policies_policy_id",
                table: "claims");

            migrationBuilder.DropForeignKey(
                name: "FK_claims_users_assigned_officer_id",
                table: "claims");

            migrationBuilder.DropForeignKey(
                name: "FK_policies_agents_agent_id",
                table: "policies");

            migrationBuilder.DropForeignKey(
                name: "FK_policies_customers_customer_id",
                table: "policies");

            migrationBuilder.DropForeignKey(
                name: "FK_policies_insurance_products_product_id",
                table: "policies");

            migrationBuilder.DropForeignKey(
                name: "FK_policies_proposals_proposal_id",
                table: "policies");

            migrationBuilder.DropForeignKey(
                name: "FK_policy_status_histories_policies_policy_id",
                table: "policy_status_histories");

            migrationBuilder.DropForeignKey(
                name: "FK_policy_status_histories_users_changed_by_id",
                table: "policy_status_histories");

            migrationBuilder.DropForeignKey(
                name: "FK_proposal_members_customer_members_customer_member_id",
                table: "proposal_members");

            migrationBuilder.DropForeignKey(
                name: "FK_proposals_agents_agent_id",
                table: "proposals");

            migrationBuilder.DropForeignKey(
                name: "FK_proposals_customers_customer_id",
                table: "proposals");

            migrationBuilder.DropForeignKey(
                name: "FK_proposals_insurance_products_product_id",
                table: "proposals");

            migrationBuilder.DropForeignKey(
                name: "FK_proposals_users_underwriter_id",
                table: "proposals");

            migrationBuilder.DropIndex(
                name: "IX_stripe_customers_user_id",
                table: "stripe_customers");

            migrationBuilder.DropIndex(
                name: "IX_policies_proposal_id",
                table: "policies");

            // Normalize legacy uppercase status strings to PascalCase before applying max-length constraint
            migrationBuilder.Sql("UPDATE proposals SET status = 'Draft' WHERE status = 'DRAFT'");
            migrationBuilder.Sql("UPDATE proposals SET status = 'Submitted' WHERE status = 'SUBMITTED'");
            migrationBuilder.Sql("UPDATE proposals SET status = 'UnderReview' WHERE status IN ('UNDER_REVIEW', 'UNDER REVIEW')");
            migrationBuilder.Sql("UPDATE proposals SET status = 'DocumentsPending' WHERE status IN ('DOCUMENTS_PENDING', 'DOCUMENT_PENDING')");
            migrationBuilder.Sql("UPDATE proposals SET status = 'Approved' WHERE status = 'APPROVED'");
            migrationBuilder.Sql("UPDATE proposals SET status = 'Rejected' WHERE status = 'REJECTED'");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "proposals",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "certifying_doctor",
                table: "life_claim_details",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "claimant_name",
                table: "life_claim_details",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "death_certificate_number",
                table: "life_claim_details",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_stripe_customers_user_id",
                table: "stripe_customers",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_policies_proposal_id",
                table: "policies",
                column: "proposal_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_agent_commissions_agents_agent_id",
                table: "agent_commissions",
                column: "agent_id",
                principalTable: "agents",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_agent_commissions_policies_policy_id",
                table: "agent_commissions",
                column: "policy_id",
                principalTable: "policies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_agent_commissions_premium_payments_payment_id",
                table: "agent_commissions",
                column: "premium_payment_id",
                principalTable: "premium_payments",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_claim_status_history_claims_claim_id",
                table: "claim_status_histories",
                column: "claim_id",
                principalTable: "claims",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_claim_status_history_users_changed_by_id",
                table: "claim_status_histories",
                column: "changed_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_claims_customer_members_claimant_member_id",
                table: "claims",
                column: "claimant_member_id",
                principalTable: "customer_members",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_claims_customers_customer_id",
                table: "claims",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_claims_policies_policy_id",
                table: "claims",
                column: "policy_id",
                principalTable: "policies",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_claims_users_assigned_officer_id",
                table: "claims",
                column: "assigned_officer_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_policies_agents_agent_id",
                table: "policies",
                column: "agent_id",
                principalTable: "agents",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_policies_customers_customer_id",
                table: "policies",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_policies_products_product_id",
                table: "policies",
                column: "product_id",
                principalTable: "insurance_products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_policies_proposals_proposal_id",
                table: "policies",
                column: "proposal_id",
                principalTable: "proposals",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_policy_status_history_policies_policy_id",
                table: "policy_status_histories",
                column: "policy_id",
                principalTable: "policies",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_policy_status_history_users_changed_by_id",
                table: "policy_status_histories",
                column: "changed_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_proposal_members_customer_members_customer_member_id",
                table: "proposal_members",
                column: "customer_member_id",
                principalTable: "customer_members",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_proposals_agents_agent_id",
                table: "proposals",
                column: "agent_id",
                principalTable: "agents",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_proposals_customers_customer_id",
                table: "proposals",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_proposals_products_product_id",
                table: "proposals",
                column: "product_id",
                principalTable: "insurance_products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_proposals_users_underwriter_id",
                table: "proposals",
                column: "underwriter_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_agent_commissions_agents_agent_id",
                table: "agent_commissions");

            migrationBuilder.DropForeignKey(
                name: "FK_agent_commissions_policies_policy_id",
                table: "agent_commissions");

            migrationBuilder.DropForeignKey(
                name: "FK_agent_commissions_premium_payments_payment_id",
                table: "agent_commissions");

            migrationBuilder.DropForeignKey(
                name: "FK_claim_status_history_claims_claim_id",
                table: "claim_status_histories");

            migrationBuilder.DropForeignKey(
                name: "FK_claim_status_history_users_changed_by_id",
                table: "claim_status_histories");

            migrationBuilder.DropForeignKey(
                name: "FK_claims_customer_members_claimant_member_id",
                table: "claims");

            migrationBuilder.DropForeignKey(
                name: "FK_claims_customers_customer_id",
                table: "claims");

            migrationBuilder.DropForeignKey(
                name: "FK_claims_policies_policy_id",
                table: "claims");

            migrationBuilder.DropForeignKey(
                name: "FK_claims_users_assigned_officer_id",
                table: "claims");

            migrationBuilder.DropForeignKey(
                name: "FK_policies_agents_agent_id",
                table: "policies");

            migrationBuilder.DropForeignKey(
                name: "FK_policies_customers_customer_id",
                table: "policies");

            migrationBuilder.DropForeignKey(
                name: "FK_policies_products_product_id",
                table: "policies");

            migrationBuilder.DropForeignKey(
                name: "FK_policies_proposals_proposal_id",
                table: "policies");

            migrationBuilder.DropForeignKey(
                name: "FK_policy_status_history_policies_policy_id",
                table: "policy_status_histories");

            migrationBuilder.DropForeignKey(
                name: "FK_policy_status_history_users_changed_by_id",
                table: "policy_status_histories");

            migrationBuilder.DropForeignKey(
                name: "FK_proposal_members_customer_members_customer_member_id",
                table: "proposal_members");

            migrationBuilder.DropForeignKey(
                name: "FK_proposals_agents_agent_id",
                table: "proposals");

            migrationBuilder.DropForeignKey(
                name: "FK_proposals_customers_customer_id",
                table: "proposals");

            migrationBuilder.DropForeignKey(
                name: "FK_proposals_products_product_id",
                table: "proposals");

            migrationBuilder.DropForeignKey(
                name: "FK_proposals_users_underwriter_id",
                table: "proposals");

            migrationBuilder.DropIndex(
                name: "IX_stripe_customers_user_id",
                table: "stripe_customers");

            migrationBuilder.DropIndex(
                name: "IX_policies_proposal_id",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "certifying_doctor",
                table: "life_claim_details");

            migrationBuilder.DropColumn(
                name: "claimant_name",
                table: "life_claim_details");

            migrationBuilder.DropColumn(
                name: "death_certificate_number",
                table: "life_claim_details");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "proposals",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_stripe_customers_user_id",
                table: "stripe_customers",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_policies_proposal_id",
                table: "policies",
                column: "proposal_id");

            migrationBuilder.AddForeignKey(
                name: "FK_agent_commissions_agents_agent_id",
                table: "agent_commissions",
                column: "agent_id",
                principalTable: "agents",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_agent_commissions_policies_policy_id",
                table: "agent_commissions",
                column: "policy_id",
                principalTable: "policies",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_agent_commissions_premium_payments_premium_payment_id",
                table: "agent_commissions",
                column: "premium_payment_id",
                principalTable: "premium_payments",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_claim_status_histories_claims_claim_id",
                table: "claim_status_histories",
                column: "claim_id",
                principalTable: "claims",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_claim_status_histories_users_changed_by_id",
                table: "claim_status_histories",
                column: "changed_by_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_claims_customer_members_claimant_member_id",
                table: "claims",
                column: "claimant_member_id",
                principalTable: "customer_members",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_claims_customers_customer_id",
                table: "claims",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_claims_policies_policy_id",
                table: "claims",
                column: "policy_id",
                principalTable: "policies",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_claims_users_assigned_officer_id",
                table: "claims",
                column: "assigned_officer_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_policies_agents_agent_id",
                table: "policies",
                column: "agent_id",
                principalTable: "agents",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_policies_customers_customer_id",
                table: "policies",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_policies_insurance_products_product_id",
                table: "policies",
                column: "product_id",
                principalTable: "insurance_products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_policies_proposals_proposal_id",
                table: "policies",
                column: "proposal_id",
                principalTable: "proposals",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_policy_status_histories_policies_policy_id",
                table: "policy_status_histories",
                column: "policy_id",
                principalTable: "policies",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_policy_status_histories_users_changed_by_id",
                table: "policy_status_histories",
                column: "changed_by_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_proposal_members_customer_members_customer_member_id",
                table: "proposal_members",
                column: "customer_member_id",
                principalTable: "customer_members",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_proposals_agents_agent_id",
                table: "proposals",
                column: "agent_id",
                principalTable: "agents",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_proposals_customers_customer_id",
                table: "proposals",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_proposals_insurance_products_product_id",
                table: "proposals",
                column: "product_id",
                principalTable: "insurance_products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_proposals_users_underwriter_id",
                table: "proposals",
                column: "underwriter_id",
                principalTable: "users",
                principalColumn: "id");
        }
    }
}
