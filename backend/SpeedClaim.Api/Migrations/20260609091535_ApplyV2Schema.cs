using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class ApplyV2Schema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_agent_commissions_payment_transactions_payment_id",
                table: "agent_commissions");

            migrationBuilder.DropForeignKey(
                name: "FK_claims_customers_customer_id",
                table: "claims");

            migrationBuilder.DropForeignKey(
                name: "FK_claims_users_assigned_adjuster_id",
                table: "claims");

            migrationBuilder.DropForeignKey(
                name: "FK_claims_users_submitted_by_id",
                table: "claims");

            migrationBuilder.DropForeignKey(
                name: "FK_policies_users_user_id",
                table: "policies");

            migrationBuilder.DropForeignKey(
                name: "FK_premium_rate_tables_insurance_products_product_id",
                table: "premium_rate_tables");

            migrationBuilder.DropForeignKey(
                name: "FK_premium_schedule_payment_transactions_payment_id",
                table: "premium_schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_premium_schedule_policies_policy_id",
                table: "premium_schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_proposal_members_customer_members_member_id",
                table: "proposal_members");

            migrationBuilder.DropTable(
                name: "claim_document_checklists");

            migrationBuilder.DropTable(
                name: "claim_health_details");

            migrationBuilder.DropTable(
                name: "claim_life_details");

            migrationBuilder.DropTable(
                name: "claim_vehicle_details");

            migrationBuilder.DropTable(
                name: "claim_workflows");

            migrationBuilder.DropTable(
                name: "payment_status_histories");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "user_consents");

            migrationBuilder.DropTable(
                name: "user_roles");

            migrationBuilder.DropTable(
                name: "policy_insured_members");

            migrationBuilder.DropTable(
                name: "payment_transactions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropIndex(
                name: "uq_system_configs_key",
                table: "system_configs");

            migrationBuilder.DropIndex(
                name: "idx_submitted_documents_entity",
                table: "submitted_documents");

            migrationBuilder.DropIndex(
                name: "uq_proposals_number",
                table: "proposals");

            migrationBuilder.DropPrimaryKey(
                name: "PK_premium_schedule",
                table: "premium_schedules");

            migrationBuilder.DropIndex(
                name: "idx_premium_schedule_due",
                table: "premium_schedules");

            migrationBuilder.DropIndex(
                name: "IX_premium_schedules_payment_id",
                table: "premium_schedules");

            migrationBuilder.DropIndex(
                name: "IX_policies_user_id",
                table: "policies");

            migrationBuilder.DropIndex(
                name: "UQ_policy_id_domain",
                table: "policies");

            migrationBuilder.DropIndex(
                name: "uq_policy_num",
                table: "policies");

            migrationBuilder.DropCheckConstraint(
                name: "CK_policies_payment_frequency",
                table: "policies");

            migrationBuilder.DropCheckConstraint(
                name: "CK_policies_status",
                table: "policies");

            migrationBuilder.DropIndex(
                name: "uq_insurance_products_code",
                table: "insurance_products");

            migrationBuilder.DropIndex(
                name: "UQ_product_id_domain",
                table: "insurance_products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_products_max_members",
                table: "insurance_products");

            migrationBuilder.DropIndex(
                name: "uq_grievances_number",
                table: "grievances");

            migrationBuilder.DropIndex(
                name: "uq_email_templates_key",
                table: "email_templates");

            migrationBuilder.DropIndex(
                name: "UQ_docreq_key_domain",
                table: "document_requirements");

            migrationBuilder.DropIndex(
                name: "UQ_claim_id_domain",
                table: "claims");

            migrationBuilder.DropIndex(
                name: "uq_claims_claim_number",
                table: "claims");

            migrationBuilder.DropCheckConstraint(
                name: "CK_claims_priority",
                table: "claims");

            migrationBuilder.DropCheckConstraint(
                name: "CK_claims_status",
                table: "claims");

            migrationBuilder.DropIndex(
                name: "idx_audit_logs_composite",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "uq_agents_license_number",
                table: "agents");

            migrationBuilder.DeleteData(
                table: "addresses",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

            migrationBuilder.DeleteData(
                table: "customers",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

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

            migrationBuilder.DeleteData(
                table: "kyc_records",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DropColumn(
                name: "profile_picture_url",
                table: "users");

            migrationBuilder.DropColumn(
                name: "coverage_amount",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "covers_dental",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "currency",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "deductible",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "domain",
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
                name: "user_id",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "vehicle_number",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "code",
                table: "insurance_products");

            migrationBuilder.DropColumn(
                name: "max_coverage",
                table: "insurance_products");

            migrationBuilder.DropColumn(
                name: "name",
                table: "insurance_products");

            migrationBuilder.DropColumn(
                name: "domain",
                table: "claims");

            migrationBuilder.DropColumn(
                name: "priority",
                table: "claims");

            migrationBuilder.DropColumn(
                name: "risk_score",
                table: "claims");

            migrationBuilder.DropColumn(
                name: "actor_id",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "user_agent",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "agency_name",
                table: "agents");

            migrationBuilder.RenameColumn(
                name: "timezone",
                table: "users",
                newName: "role");

            migrationBuilder.RenameIndex(
                name: "uq_stripe_customers_id",
                table: "stripe_customers",
                newName: "IX_stripe_customers_stripe_customer_id");

            migrationBuilder.RenameColumn(
                name: "max_insured_members",
                table: "insurance_products",
                newName: "waiting_period_days");

            migrationBuilder.RenameColumn(
                name: "allows_insured_members",
                table: "insurance_products",
                newName: "allows_family_floater");

            migrationBuilder.RenameColumn(
                name: "submitted_by_id",
                table: "claims",
                newName: "claimant_member_id");

            migrationBuilder.RenameColumn(
                name: "is_automated_processed",
                table: "claims",
                newName: "is_cashless");

            migrationBuilder.RenameColumn(
                name: "claimed_amount",
                table: "claims",
                newName: "claim_amount_requested");

            migrationBuilder.RenameColumn(
                name: "assigned_adjuster_id",
                table: "claims",
                newName: "surveyor_id");

            migrationBuilder.RenameColumn(
                name: "approved_amount",
                table: "claims",
                newName: "claim_amount_approved");

            migrationBuilder.RenameIndex(
                name: "IX_claims_submitted_by_id",
                table: "claims",
                newName: "IX_claims_claimant_member_id");

            migrationBuilder.RenameIndex(
                name: "IX_claims_assigned_adjuster_id",
                table: "claims",
                newName: "IX_claims_surveyor_id");

            migrationBuilder.RenameColumn(
                name: "old_values",
                table: "audit_logs",
                newName: "old_value");

            migrationBuilder.RenameColumn(
                name: "new_values",
                table: "audit_logs",
                newName: "new_value");

            migrationBuilder.RenameColumn(
                name: "postal_code",
                table: "addresses",
                newName: "pincode");

            migrationBuilder.RenameColumn(
                name: "line2",
                table: "addresses",
                newName: "address_line2");

            migrationBuilder.RenameColumn(
                name: "line1",
                table: "addresses",
                newName: "address_line1");

            migrationBuilder.AddColumn<bool>(
                name: "is_email_verified",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "config_key",
                table: "system_configs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "entity_type",
                table: "submitted_documents",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "policy_type",
                table: "proposals",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "premium_schedules",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<decimal>(
                name: "amount_due",
                table: "premium_schedules",
                type: "numeric(14,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "policies",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<decimal>(
                name: "premium_amount",
                table: "policies",
                type: "numeric(14,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<string>(
                name: "policy_type",
                table: "policies",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "policy_number",
                table: "policies",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "payment_frequency",
                table: "policies",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<decimal>(
                name: "sum_assured",
                table: "policies",
                type: "numeric(14,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "kyc_status",
                table: "kyc_records",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "id_type",
                table: "kyc_records",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "domain",
                table: "insurance_products",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                table: "insurance_products",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "created_by_id",
                table: "insurance_products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_age",
                table: "insurance_products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "max_family_members",
                table: "insurance_products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "max_sum_assured",
                table: "insurance_products",
                type: "numeric(14,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "max_tenure_years",
                table: "insurance_products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "min_age",
                table: "insurance_products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "min_sum_assured",
                table: "insurance_products",
                type: "numeric(14,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "min_tenure_years",
                table: "insurance_products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "product_name",
                table: "insurance_products",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "uin",
                table: "insurance_products",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "insurance_products",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "grievances",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "category",
                table: "grievances",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "endorsement_type",
                table: "endorsements",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "label",
                table: "document_requirements",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "entity_type",
                table: "document_requirements",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "domain",
                table: "document_requirements",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "document_key",
                table: "document_requirements",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "marital_status",
                table: "customers",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "gender",
                table: "customers",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "relationship",
                table: "customer_members",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "gender",
                table: "customer_members",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "claims",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "claims",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "claim_number",
                table: "claims",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<Guid>(
                name: "assigned_officer_id",
                table: "claims",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "claim_type",
                table: "claims",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "intimation_date",
                table: "claims",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "settlement_date",
                table: "claims",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "state",
                table: "branches",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "city",
                table: "branches",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "entity_type",
                table: "audit_logs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "action",
                table: "audit_logs",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "agent_type",
                table: "agents",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "address_type",
                table: "addresses",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddPrimaryKey(
                name: "PK_premium_schedules",
                table: "premium_schedules",
                column: "id");

            migrationBuilder.CreateTable(
                name: "claim_status_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_id = table.Column<Guid>(type: "uuid", nullable: false),
                    old_status = table.Column<string>(type: "text", nullable: false),
                    new_status = table.Column<string>(type: "text", nullable: false),
                    changed_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_claim_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_claim_status_histories_claims_claim_id",
                        column: x => x.claim_id,
                        principalTable: "claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_claim_status_histories_users_changed_by_id",
                        column: x => x.changed_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "health_claim_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hospital_name = table.Column<string>(type: "text", nullable: false),
                    hospital_address = table.Column<string>(type: "text", nullable: false),
                    admission_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    discharge_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    diagnosis = table.Column<string>(type: "text", nullable: false),
                    treatment_type = table.Column<string>(type: "text", nullable: false),
                    tpa_reference_number = table.Column<string>(type: "text", nullable: false),
                    pre_auth_requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    pre_auth_approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_health_claim_details", x => x.id);
                    table.ForeignKey(
                        name: "FK_health_claim_details_claims",
                        column: x => x.claim_id,
                        principalTable: "claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "health_policy_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pre_existing_conditions = table.Column<string>(type: "text", nullable: false),
                    network_hospital_coverage = table.Column<string>(type: "text", nullable: false),
                    tpa_name = table.Column<string>(type: "text", nullable: false),
                    room_rent_limit = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    maternity_covered = table.Column<bool>(type: "boolean", nullable: false),
                    copay_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false)
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
                name: "life_claim_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_reason = table.Column<string>(type: "text", nullable: false),
                    cause_of_death = table.Column<string>(type: "text", nullable: false),
                    place_of_death = table.Column<string>(type: "text", nullable: false),
                    claimant_relationship = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_life_claim_details", x => x.id);
                    table.ForeignKey(
                        name: "FK_life_claim_details_claims",
                        column: x => x.claim_id,
                        principalTable: "claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "life_policy_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_subtype = table.Column<string>(type: "text", nullable: false),
                    maturity_benefit = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    death_benefit = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    surrender_value_applicable = table.Column<bool>(type: "boolean", nullable: false),
                    loan_eligible = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "motor_claim_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accident_location = table.Column<string>(type: "text", nullable: false),
                    fir_number = table.Column<string>(type: "text", nullable: true),
                    garage_name = table.Column<string>(type: "text", nullable: false),
                    estimated_repair_cost = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    survey_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    surveyor_remarks = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_motor_claim_details", x => x.id);
                    table.ForeignKey(
                        name: "FK_motor_claim_details_claims",
                        column: x => x.claim_id,
                        principalTable: "claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "motor_policy_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_number = table.Column<string>(type: "text", nullable: false),
                    vehicle_make = table.Column<string>(type: "text", nullable: false),
                    vehicle_model = table.Column<string>(type: "text", nullable: false),
                    manufacture_year = table.Column<int>(type: "integer", nullable: false),
                    vehicle_type = table.Column<string>(type: "text", nullable: false),
                    idv = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    engine_number = table.Column<string>(type: "text", nullable: false),
                    chassis_number = table.Column<string>(type: "text", nullable: false),
                    cover_type = table.Column<string>(type: "text", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "policy_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_policy_members", x => x.id);
                    table.ForeignKey(
                        name: "FK_policy_members_customer_members_customer_member_id",
                        column: x => x.customer_member_id,
                        principalTable: "customer_members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_policy_members_policies",
                        column: x => x.policy_id,
                        principalTable: "policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "policy_status_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    old_status = table.Column<string>(type: "text", nullable: false),
                    new_status = table.Column<string>(type: "text", nullable: false),
                    changed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_policy_status_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_policy_status_histories_policies_policy_id",
                        column: x => x.policy_id,
                        principalTable: "policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_policy_status_histories_users_changed_by_id",
                        column: x => x.changed_by_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "premium_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    schedule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    payment_type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    stripe_payment_intent_id = table.Column<string>(type: "text", nullable: false),
                    stripe_charge_id = table.Column<string>(type: "text", nullable: false),
                    payment_method = table.Column<string>(type: "text", nullable: false),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    receipt_url = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_premium_payments", x => x.id);
                    table.ForeignKey(
                        name: "FK_premium_payments_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_premium_payments_policies_policy_id",
                        column: x => x.policy_id,
                        principalTable: "policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_premium_payments_schedules",
                        column: x => x.schedule_id,
                        principalTable: "premium_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    refresh_token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ip_address = table.Column<string>(type: "text", nullable: false),
                    user_agent = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_sessions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "customers",
                columns: new[] { "id", "annual_income", "created_at", "date_of_birth", "gender", "marital_status", "occupation", "updated_at", "user_id" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), 150000m, new DateTimeOffset(new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTime(1995, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Male", "Single", "Software Engineer", null, new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.UpdateData(
                table: "document_requirements",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "entity_type",
                value: "Kyc");

            migrationBuilder.UpdateData(
                table: "document_requirements",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "entity_type",
                value: "Kyc");

            migrationBuilder.InsertData(
                table: "insurance_products",
                columns: new[] { "id", "allows_family_floater", "created_at", "created_by_id", "description", "domain", "is_active", "max_age", "max_family_members", "max_sum_assured", "max_tenure_years", "min_age", "min_sum_assured", "min_tenure_years", "product_name", "uin", "updated_at", "waiting_period_days" },
                values: new object[] { new Guid("10000000-1111-1111-1111-111111111111"), false, new DateTimeOffset(new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Basic term life insurance", "LIFE", true, 60, 1, 5000000m, 30, 18, 100000m, 5, "Term Life Basic", "UIN123", null, 0 });

            migrationBuilder.InsertData(
                table: "kyc_records",
                columns: new[] { "id", "created_at", "id_number", "id_type", "kyc_status", "rejection_reason", "reviewed_at", "reviewed_by_id", "updated_at", "user_id" },
                values: new object[] { new Guid("33333333-3333-3333-3333-333333333333"), new DateTimeOffset(new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "999988887777", "Aadhaar", "Approved", null, null, null, null, new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "is_email_verified", "role" },
                values: new object[] { true, "Admin" });

            migrationBuilder.CreateIndex(
                name: "IX_insurance_products_created_by_id",
                table: "insurance_products",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "uq_insurance_products_uin",
                table: "insurance_products",
                column: "uin",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_claims_assigned_officer_id",
                table: "claims",
                column: "assigned_officer_id");

            migrationBuilder.CreateIndex(
                name: "IX_claim_status_histories_changed_by_id",
                table: "claim_status_histories",
                column: "changed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_claim_status_histories_claim_id",
                table: "claim_status_histories",
                column: "claim_id");

            migrationBuilder.CreateIndex(
                name: "IX_health_claim_details_claim_id",
                table: "health_claim_details",
                column: "claim_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_health_policy_details_policy_id",
                table: "health_policy_details",
                column: "policy_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_life_claim_details_claim_id",
                table: "life_claim_details",
                column: "claim_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_life_policy_details_policy_id",
                table: "life_policy_details",
                column: "policy_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_motor_claim_details_claim_id",
                table: "motor_claim_details",
                column: "claim_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_motor_policy_details_policy_id",
                table: "motor_policy_details",
                column: "policy_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_policy_members_customer_member_id",
                table: "policy_members",
                column: "customer_member_id");

            migrationBuilder.CreateIndex(
                name: "IX_policy_members_policy_id",
                table: "policy_members",
                column: "policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_policy_status_histories_changed_by_id",
                table: "policy_status_histories",
                column: "changed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_policy_status_histories_policy_id",
                table: "policy_status_histories",
                column: "policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_premium_payments_customer_id",
                table: "premium_payments",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_premium_payments_policy_id",
                table: "premium_payments",
                column: "policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_premium_payments_schedule_id",
                table: "premium_payments",
                column: "schedule_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sessions_user_id",
                table: "sessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_sessions_token_hash",
                table: "sessions",
                column: "refresh_token_hash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_agent_commissions_premium_payments_premium_payment_id",
                table: "agent_commissions",
                column: "premium_payment_id",
                principalTable: "premium_payments",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_claims_customer_members_claimant_member_id",
                table: "claims",
                column: "claimant_member_id",
                principalTable: "customer_members",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_claims_customers_customer_id",
                table: "claims",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_claims_users_assigned_officer_id",
                table: "claims",
                column: "assigned_officer_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_claims_users_surveyor_id",
                table: "claims",
                column: "surveyor_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_insurance_products_users_created_by_id",
                table: "insurance_products",
                column: "created_by_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_premium_rate_tables_products",
                table: "premium_rate_tables",
                column: "product_id",
                principalTable: "insurance_products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_premium_schedules_policies_policy_id",
                table: "premium_schedules",
                column: "policy_id",
                principalTable: "policies",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_proposal_members_customer_members_customer_member_id",
                table: "proposal_members",
                column: "customer_member_id",
                principalTable: "customer_members",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_agent_commissions_premium_payments_premium_payment_id",
                table: "agent_commissions");

            migrationBuilder.DropForeignKey(
                name: "FK_claims_customer_members_claimant_member_id",
                table: "claims");

            migrationBuilder.DropForeignKey(
                name: "FK_claims_customers_customer_id",
                table: "claims");

            migrationBuilder.DropForeignKey(
                name: "FK_claims_users_assigned_officer_id",
                table: "claims");

            migrationBuilder.DropForeignKey(
                name: "FK_claims_users_surveyor_id",
                table: "claims");

            migrationBuilder.DropForeignKey(
                name: "FK_insurance_products_users_created_by_id",
                table: "insurance_products");

            migrationBuilder.DropForeignKey(
                name: "FK_premium_rate_tables_products",
                table: "premium_rate_tables");

            migrationBuilder.DropForeignKey(
                name: "FK_premium_schedules_policies_policy_id",
                table: "premium_schedules");

            migrationBuilder.DropForeignKey(
                name: "FK_proposal_members_customer_members_customer_member_id",
                table: "proposal_members");

            migrationBuilder.DropTable(
                name: "claim_status_histories");

            migrationBuilder.DropTable(
                name: "health_claim_details");

            migrationBuilder.DropTable(
                name: "health_policy_details");

            migrationBuilder.DropTable(
                name: "life_claim_details");

            migrationBuilder.DropTable(
                name: "life_policy_details");

            migrationBuilder.DropTable(
                name: "motor_claim_details");

            migrationBuilder.DropTable(
                name: "motor_policy_details");

            migrationBuilder.DropTable(
                name: "policy_members");

            migrationBuilder.DropTable(
                name: "policy_status_histories");

            migrationBuilder.DropTable(
                name: "premium_payments");

            migrationBuilder.DropTable(
                name: "sessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_premium_schedules",
                table: "premium_schedules");

            migrationBuilder.DropIndex(
                name: "IX_insurance_products_created_by_id",
                table: "insurance_products");

            migrationBuilder.DropIndex(
                name: "uq_insurance_products_uin",
                table: "insurance_products");

            migrationBuilder.DropIndex(
                name: "IX_claims_assigned_officer_id",
                table: "claims");

            migrationBuilder.DeleteData(
                table: "customers",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "insurance_products",
                keyColumn: "id",
                keyValue: new Guid("10000000-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "kyc_records",
                keyColumn: "id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));

            migrationBuilder.DropColumn(
                name: "is_email_verified",
                table: "users");

            migrationBuilder.DropColumn(
                name: "sum_assured",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "insurance_products");

            migrationBuilder.DropColumn(
                name: "created_by_id",
                table: "insurance_products");

            migrationBuilder.DropColumn(
                name: "max_age",
                table: "insurance_products");

            migrationBuilder.DropColumn(
                name: "max_family_members",
                table: "insurance_products");

            migrationBuilder.DropColumn(
                name: "max_sum_assured",
                table: "insurance_products");

            migrationBuilder.DropColumn(
                name: "max_tenure_years",
                table: "insurance_products");

            migrationBuilder.DropColumn(
                name: "min_age",
                table: "insurance_products");

            migrationBuilder.DropColumn(
                name: "min_sum_assured",
                table: "insurance_products");

            migrationBuilder.DropColumn(
                name: "min_tenure_years",
                table: "insurance_products");

            migrationBuilder.DropColumn(
                name: "product_name",
                table: "insurance_products");

            migrationBuilder.DropColumn(
                name: "uin",
                table: "insurance_products");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "insurance_products");

            migrationBuilder.DropColumn(
                name: "assigned_officer_id",
                table: "claims");

            migrationBuilder.DropColumn(
                name: "claim_type",
                table: "claims");

            migrationBuilder.DropColumn(
                name: "intimation_date",
                table: "claims");

            migrationBuilder.DropColumn(
                name: "settlement_date",
                table: "claims");

            migrationBuilder.RenameColumn(
                name: "role",
                table: "users",
                newName: "timezone");

            migrationBuilder.RenameIndex(
                name: "IX_stripe_customers_stripe_customer_id",
                table: "stripe_customers",
                newName: "uq_stripe_customers_id");

            migrationBuilder.RenameColumn(
                name: "waiting_period_days",
                table: "insurance_products",
                newName: "max_insured_members");

            migrationBuilder.RenameColumn(
                name: "allows_family_floater",
                table: "insurance_products",
                newName: "allows_insured_members");

            migrationBuilder.RenameColumn(
                name: "surveyor_id",
                table: "claims",
                newName: "assigned_adjuster_id");

            migrationBuilder.RenameColumn(
                name: "is_cashless",
                table: "claims",
                newName: "is_automated_processed");

            migrationBuilder.RenameColumn(
                name: "claimant_member_id",
                table: "claims",
                newName: "submitted_by_id");

            migrationBuilder.RenameColumn(
                name: "claim_amount_requested",
                table: "claims",
                newName: "claimed_amount");

            migrationBuilder.RenameColumn(
                name: "claim_amount_approved",
                table: "claims",
                newName: "approved_amount");

            migrationBuilder.RenameIndex(
                name: "IX_claims_surveyor_id",
                table: "claims",
                newName: "IX_claims_assigned_adjuster_id");

            migrationBuilder.RenameIndex(
                name: "IX_claims_claimant_member_id",
                table: "claims",
                newName: "IX_claims_submitted_by_id");

            migrationBuilder.RenameColumn(
                name: "old_value",
                table: "audit_logs",
                newName: "old_values");

            migrationBuilder.RenameColumn(
                name: "new_value",
                table: "audit_logs",
                newName: "new_values");

            migrationBuilder.RenameColumn(
                name: "pincode",
                table: "addresses",
                newName: "postal_code");

            migrationBuilder.RenameColumn(
                name: "address_line2",
                table: "addresses",
                newName: "line2");

            migrationBuilder.RenameColumn(
                name: "address_line1",
                table: "addresses",
                newName: "line1");

            migrationBuilder.AddColumn<string>(
                name: "profile_picture_url",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "config_key",
                table: "system_configs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "entity_type",
                table: "submitted_documents",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "policy_type",
                table: "proposals",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "premium_schedules",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "amount_due",
                table: "premium_schedules",
                type: "numeric(12,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "policies",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "premium_amount",
                table: "policies",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(14,2)");

            migrationBuilder.AlterColumn<int>(
                name: "policy_type",
                table: "policies",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "policy_number",
                table: "policies",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "payment_frequency",
                table: "policies",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<decimal>(
                name: "coverage_amount",
                table: "policies",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "covers_dental",
                table: "policies",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "currency",
                table: "policies",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "deductible",
                table: "policies",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                table: "policies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "domain",
                table: "policies",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

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

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "policies",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "vehicle_number",
                table: "policies",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "kyc_status",
                table: "kyc_records",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "id_type",
                table: "kyc_records",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "domain",
                table: "insurance_products",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "insurance_products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "max_coverage",
                table: "insurance_products",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "insurance_products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "grievances",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "category",
                table: "grievances",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "endorsement_type",
                table: "endorsements",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "label",
                table: "document_requirements",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "entity_type",
                table: "document_requirements",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "domain",
                table: "document_requirements",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "document_key",
                table: "document_requirements",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "marital_status",
                table: "customers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "gender",
                table: "customers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "relationship",
                table: "customer_members",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "gender",
                table: "customer_members",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "claims",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_id",
                table: "claims",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<string>(
                name: "claim_number",
                table: "claims",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "domain",
                table: "claims",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<short>(
                name: "priority",
                table: "claims",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<decimal>(
                name: "risk_score",
                table: "claims",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0.00m);

            migrationBuilder.AlterColumn<string>(
                name: "state",
                table: "branches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "city",
                table: "branches",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "entity_type",
                table: "audit_logs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "action",
                table: "audit_logs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<Guid>(
                name: "actor_id",
                table: "audit_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_agent",
                table: "audit_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "agent_type",
                table: "agents",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "agency_name",
                table: "agents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<int>(
                name: "address_type",
                table: "addresses",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_premium_schedule",
                table: "premium_schedules",
                column: "id");

            migrationBuilder.CreateTable(
                name: "claim_document_checklists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    domain = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                name: "claim_life_details",
                columns: table => new
                {
                    claim_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cause_of_death = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    certifying_doctor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    claimant_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    claimant_relation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    death_certificate_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    place_of_death = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
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
                    is_total_loss = table.Column<bool>(type: "boolean", nullable: false),
                    repair_estimate = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
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
                name: "claim_workflows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    remarks = table.Column<string>(type: "text", nullable: true),
                    to_status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
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
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    stripe_event_id = table.Column<string>(type: "text", nullable: false),
                    stripe_payment_intent_id = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_transactions_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_payment_transactions_policies_policy_id",
                        column: x => x.policy_id,
                        principalTable: "policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "policy_insured_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    address_id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    relation_to_holder = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    salutation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_policy_insured_members", x => x.id);
                    table.ForeignKey(
                        name: "FK_policy_insured_members_addresses_address_id",
                        column: x => x.address_id,
                        principalTable: "addresses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_policy_insured_members_policies_policy_id",
                        column: x => x.policy_id,
                        principalTable: "policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ip_address = table.Column<string>(type: "text", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    hierarchy_level = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                    table.CheckConstraint("CK_roles_hierarchy_level", "hierarchy_level > 0");
                });

            migrationBuilder.CreateTable(
                name: "user_consents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consent_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    consent_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    is_granted = table.Column<bool>(type: "boolean", nullable: false),
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

            migrationBuilder.CreateTable(
                name: "payment_status_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    new_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    old_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    remarks = table.Column<string>(type: "text", nullable: true)
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
                name: "claim_health_details",
                columns: table => new
                {
                    claim_id = table.Column<Guid>(type: "uuid", nullable: false),
                    insured_member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    admission_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    diagnosis = table.Column<string>(type: "text", nullable: false),
                    discharge_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    hospital_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_cashless = table.Column<bool>(type: "boolean", nullable: false),
                    treating_doctor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
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
                name: "user_roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_limit = table.Column<decimal>(type: "numeric", nullable: true),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    domain = table.Column<string>(type: "text", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_roles_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_roles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "addresses",
                columns: new[] { "id", "address_type", "city", "country", "created_at", "is_same_as_permanent", "line1", "line2", "postal_code", "state", "updated_at", "user_id" },
                values: new object[] { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), 0, "Admin City", "India", new DateTimeOffset(new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), false, "123 Admin St", null, "123456", "Admin State", null, new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.InsertData(
                table: "customers",
                columns: new[] { "id", "annual_income", "created_at", "date_of_birth", "gender", "marital_status", "occupation", "updated_at", "user_id" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), 0m, new DateTimeOffset(new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTime(1995, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, 0, "", null, new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.UpdateData(
                table: "document_requirements",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000001"),
                column: "entity_type",
                value: 0);

            migrationBuilder.UpdateData(
                table: "document_requirements",
                keyColumn: "id",
                keyValue: new Guid("10000000-0000-0000-0000-000000000002"),
                column: "entity_type",
                value: 0);

            migrationBuilder.InsertData(
                table: "insurance_products",
                columns: new[] { "id", "allows_insured_members", "code", "description", "domain", "is_active", "max_coverage", "max_insured_members", "name" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), true, "PRD-LIFE-001", "Basic term life insurance", "LIFE", true, 100000m, 1, "Term Life Basic" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), true, "PRD-HLTH-001", "Comprehensive health insurance", "HEALTH", true, 50000m, 5, "Health Shield" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), false, "PRD-AUTO-001", "Full coverage auto insurance", "AUTO", true, 30000m, 0, "Auto Protect" }
                });

            migrationBuilder.InsertData(
                table: "kyc_records",
                columns: new[] { "id", "created_at", "id_number", "id_type", "kyc_status", "rejection_reason", "reviewed_at", "reviewed_by_id", "updated_at", "user_id" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "999988887777", 0, 2, null, null, null, null, new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "code", "description", "hierarchy_level" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "Customer", "Standard customer", 10 },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Agent", "Insurance agent", 20 },
                    { new Guid("33333333-3333-3333-3333-333333333333"), "Admin", "System administrator", 50 }
                });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "profile_picture_url", "timezone" },
                values: new object[] { "", "UTC" });

            migrationBuilder.InsertData(
                table: "user_roles",
                columns: new[] { "id", "approval_limit", "assigned_at", "domain", "revoked_at", "role_id", "user_id" },
                values: new object[] { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), null, new DateTimeOffset(new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "AUTH", null, new Guid("33333333-3333-3333-3333-333333333333"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.CreateIndex(
                name: "uq_system_configs_key",
                table: "system_configs",
                column: "config_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_submitted_documents_entity",
                table: "submitted_documents",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "uq_proposals_number",
                table: "proposals",
                column: "proposal_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_premium_schedule_due",
                table: "premium_schedules",
                columns: new[] { "due_date", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_premium_schedules_payment_id",
                table: "premium_schedules",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_policies_user_id",
                table: "policies",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "UQ_policy_id_domain",
                table: "policies",
                columns: new[] { "id", "domain" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_policy_num",
                table: "policies",
                column: "policy_number",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_policies_payment_frequency",
                table: "policies",
                sql: "payment_frequency IN ('MONTHLY', 'QUARTERLY', 'ANNUAL')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_policies_status",
                table: "policies",
                sql: "status IN ('Pending', 'Active', 'Lapsed', 'Cancelled', 'Expired', 'Claimed')");

            migrationBuilder.CreateIndex(
                name: "uq_insurance_products_code",
                table: "insurance_products",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_product_id_domain",
                table: "insurance_products",
                columns: new[] { "id", "domain" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_products_max_members",
                table: "insurance_products",
                sql: "max_insured_members >= 0");

            migrationBuilder.CreateIndex(
                name: "uq_grievances_number",
                table: "grievances",
                column: "grievance_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_email_templates_key",
                table: "email_templates",
                column: "template_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_docreq_key_domain",
                table: "document_requirements",
                columns: new[] { "document_key", "domain" },
                unique: true);

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

            migrationBuilder.AddCheckConstraint(
                name: "CK_claims_priority",
                table: "claims",
                sql: "priority IN (1, 2, 3)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_claims_status",
                table: "claims",
                sql: "status IN ('SUBMITTED', 'UNDER_REVIEW', 'ESCALATED', 'APPROVED', 'REJECTED', 'SETTLED', 'CLOSED')");

            migrationBuilder.CreateIndex(
                name: "idx_audit_logs_composite",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "uq_agents_license_number",
                table: "agents",
                column: "license_number",
                unique: true);

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
                name: "IX_claim_workflows_actor_id",
                table: "claim_workflows",
                column: "actor_id");

            migrationBuilder.CreateIndex(
                name: "IX_claim_workflows_claim_id",
                table: "claim_workflows",
                column: "claim_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_status_histories_changed_by_id",
                table: "payment_status_histories",
                column: "changed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_status_histories_payment_id",
                table: "payment_status_histories",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_customer_id",
                table: "payment_transactions",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_policy_id",
                table: "payment_transactions",
                column: "policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_stripe_payment_intent_id",
                table: "payment_transactions",
                column: "stripe_payment_intent_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_policy_insured_members_address_id",
                table: "policy_insured_members",
                column: "address_id");

            migrationBuilder.CreateIndex(
                name: "IX_policy_insured_members_policy_id",
                table: "policy_insured_members",
                column: "policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_roles_code",
                table: "roles",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_consents_user_id",
                table: "user_consents",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_role_id",
                table: "user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_user_id",
                table: "user_roles",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_agent_commissions_payment_transactions_payment_id",
                table: "agent_commissions",
                column: "premium_payment_id",
                principalTable: "payment_transactions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_claims_customers_customer_id",
                table: "claims",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_claims_users_assigned_adjuster_id",
                table: "claims",
                column: "assigned_adjuster_id",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_claims_users_submitted_by_id",
                table: "claims",
                column: "submitted_by_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_policies_users_user_id",
                table: "policies",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_premium_rate_tables_insurance_products_product_id",
                table: "premium_rate_tables",
                column: "product_id",
                principalTable: "insurance_products",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_premium_schedule_payment_transactions_payment_id",
                table: "premium_schedules",
                column: "payment_id",
                principalTable: "payment_transactions",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_premium_schedule_policies_policy_id",
                table: "premium_schedules",
                column: "policy_id",
                principalTable: "policies",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_proposal_members_customer_members_member_id",
                table: "proposal_members",
                column: "customer_member_id",
                principalTable: "customer_members",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
