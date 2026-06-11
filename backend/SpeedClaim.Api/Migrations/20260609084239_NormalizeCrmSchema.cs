using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeCrmSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_addresses_current_address_id",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "FK_users_addresses_permanent_address_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "document_types");

            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropIndex(
                name: "IX_users_current_address_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_permanent_address_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "uq_users_aadhaar",
                table: "users");

            migrationBuilder.DropIndex(
                name: "uq_users_pan",
                table: "users");

            migrationBuilder.DropColumn(
                name: "aadhaar_number",
                table: "users");

            migrationBuilder.DropColumn(
                name: "current_address_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "gender",
                table: "users");

            migrationBuilder.DropColumn(
                name: "kyc_status",
                table: "users");

            migrationBuilder.DropColumn(
                name: "marital_status",
                table: "users");

            migrationBuilder.DropColumn(
                name: "pan_number",
                table: "users");

            migrationBuilder.DropColumn(
                name: "permanent_address_id",
                table: "users");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                table: "users",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "date_of_birth",
                table: "users",
                newName: "last_login_at");

            migrationBuilder.RenameColumn(
                name: "license_valid_until",
                table: "agents",
                newName: "license_expiry");

            migrationBuilder.AddColumn<Guid>(
                name: "customer_id",
                table: "policies",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "issued_at",
                table: "policies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "policy_type",
                table: "policies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "proposal_id",
                table: "policies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "policies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "customer_id",
                table: "payment_transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "customer_id",
                table: "claims",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "agent_code",
                table: "agents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "agent_type",
                table: "agents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "branch_id",
                table: "agents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "agents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "address_type",
                table: "addresses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                table: "addresses",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "is_same_as_permanent",
                table: "addresses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "updated_at",
                table: "addresses",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "addresses",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "agent_commissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    premium_payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    commission_rate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    commission_amount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    paid_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_agent_commissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_agent_commissions_agents_agent_id",
                        column: x => x.agent_id,
                        principalTable: "agents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_agent_commissions_payment_transactions_payment_id",
                        column: x => x.premium_payment_id,
                        principalTable: "payment_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_agent_commissions_policies_policy_id",
                        column: x => x.policy_id,
                        principalTable: "policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "branches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_branches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    gender = table.Column<int>(type: "integer", nullable: false),
                    occupation = table.Column<string>(type: "text", nullable: false),
                    annual_income = table.Column<decimal>(type: "numeric", nullable: false),
                    marital_status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.id);
                    table.ForeignKey(
                        name: "FK_customers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "document_requirements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<int>(type: "integer", nullable: false),
                    domain = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    document_key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    label = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_requirements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recipient_email = table.Column<string>(type: "text", nullable: false),
                    template_key = table.Column<string>(type: "text", nullable: false),
                    subject = table.Column<string>(type: "text", nullable: false),
                    variables_used = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    provider_message_id = table.Column<string>(type: "text", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    sent_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_email_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "email_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_key = table.Column<string>(type: "text", nullable: false),
                    subject = table.Column<string>(type: "text", nullable: false),
                    body_html = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "endorsements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    endorsement_type = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    old_value = table.Column<string>(type: "jsonb", nullable: true),
                    new_value = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    requested_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_endorsements", x => x.id);
                    table.ForeignKey(
                        name: "FK_endorsements_policies_policy_id",
                        column: x => x.policy_id,
                        principalTable: "policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_endorsements_users_requested_by_id",
                        column: x => x.requested_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_endorsements_users_reviewed_by_id",
                        column: x => x.reviewed_by_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "kyc_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kyc_status = table.Column<int>(type: "integer", nullable: false),
                    id_type = table.Column<int>(type: "integer", nullable: false),
                    id_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    reviewed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kyc_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_kyc_records_users_reviewed_by_id",
                        column: x => x.reviewed_by_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_kyc_records_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "nominees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "text", nullable: false),
                    relationship = table.Column<string>(type: "text", nullable: false),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    share_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    is_minor = table.Column<bool>(type: "boolean", nullable: false),
                    appointee_name = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nominees", x => x.id);
                    table.ForeignKey(
                        name: "FK_nominees_policies_policy_id",
                        column: x => x.policy_id,
                        principalTable: "policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    message = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    redirect_url = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                    table.ForeignKey(
                        name: "FK_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "premium_rate_tables",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    age_min = table.Column<int>(type: "integer", nullable: false),
                    age_max = table.Column<int>(type: "integer", nullable: false),
                    sum_assured_min = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    sum_assured_max = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    annual_premium = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_premium_rate_tables", x => x.id);
                    table.ForeignKey(
                        name: "FK_premium_rate_tables_insurance_products_product_id",
                        column: x => x.product_id,
                        principalTable: "insurance_products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "stripe_customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stripe_customer_id = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stripe_customers", x => x.id);
                    table.ForeignKey(
                        name: "FK_stripe_customers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "submitted_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<int>(type: "integer", nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_key = table.Column<string>(type: "text", nullable: false),
                    original_filename = table.Column<string>(type: "text", nullable: false),
                    stored_filename = table.Column<string>(type: "text", nullable: false),
                    file_path = table.Column<string>(type: "text", nullable: false),
                    file_size_kb = table.Column<int>(type: "integer", nullable: false),
                    mime_type = table.Column<string>(type: "text", nullable: false),
                    uploaded_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    claim_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_submitted_documents", x => x.id);
                    table.ForeignKey(
                        name: "FK_submitted_documents_claims_claim_id",
                        column: x => x.claim_id,
                        principalTable: "claims",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_submitted_documents_users_uploaded_by_id",
                        column: x => x.uploaded_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "system_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    config_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    config_value = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    updated_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_configs", x => x.id);
                    table.ForeignKey(
                        name: "FK_system_configs_users_updated_by_id",
                        column: x => x.updated_by_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "customer_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    salutation = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    gender = table.Column<int>(type: "integer", nullable: false),
                    relationship = table.Column<int>(type: "integer", nullable: false),
                    is_dependent = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customer_members", x => x.id);
                    table.ForeignKey(
                        name: "FK_customer_members_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "grievances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    grievance_number = table.Column<string>(type: "text", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    claim_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    assigned_to_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolution_notes = table.Column<string>(type: "text", nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grievances", x => x.id);
                    table.ForeignKey(
                        name: "FK_grievances_claims_claim_id",
                        column: x => x.claim_id,
                        principalTable: "claims",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_grievances_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_grievances_policies_policy_id",
                        column: x => x.policy_id,
                        principalTable: "policies",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_grievances_users_assigned_to_id",
                        column: x => x.assigned_to_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "proposals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_number = table.Column<string>(type: "text", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_type = table.Column<int>(type: "integer", nullable: false),
                    sum_assured = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    tenure_years = table.Column<int>(type: "integer", nullable: false),
                    premium_amount = table.Column<decimal>(type: "numeric(14,2)", nullable: false),
                    payment_frequency = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    underwriter_id = table.Column<Guid>(type: "uuid", nullable: true),
                    underwriter_notes = table.Column<string>(type: "text", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reviewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proposals", x => x.id);
                    table.ForeignKey(
                        name: "FK_proposals_agents_agent_id",
                        column: x => x.agent_id,
                        principalTable: "agents",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_proposals_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_proposals_insurance_products_product_id",
                        column: x => x.product_id,
                        principalTable: "insurance_products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_proposals_users_underwriter_id",
                        column: x => x.underwriter_id,
                        principalTable: "users",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "proposal_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_proposal_members", x => x.id);
                    table.ForeignKey(
                        name: "FK_proposal_members_customer_members_member_id",
                        column: x => x.customer_member_id,
                        principalTable: "customer_members",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_proposal_members_proposals_proposal_id",
                        column: x => x.proposal_id,
                        principalTable: "proposals",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "addresses",
                keyColumn: "id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "address_type", "created_at", "is_same_as_permanent", "updated_at", "user_id" },
                values: new object[] { 0, new DateTimeOffset(new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), false, null, new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.InsertData(
                table: "customers",
                columns: new[] { "id", "annual_income", "created_at", "date_of_birth", "gender", "marital_status", "occupation", "updated_at", "user_id" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), 0m, new DateTimeOffset(new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTime(1995, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, 0, "", null, new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.InsertData(
                table: "document_requirements",
                columns: new[] { "id", "created_at", "description", "document_key", "domain", "entity_type", "is_active", "is_mandatory", "label" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "", "AADHAAR", "ALL", 0, true, false, "Aadhaar Card" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "", "PAN", "ALL", 0, true, false, "PAN Card" }
                });

            migrationBuilder.InsertData(
                table: "kyc_records",
                columns: new[] { "id", "created_at", "id_number", "id_type", "kyc_status", "rejection_reason", "reviewed_at", "reviewed_by_id", "updated_at", "user_id" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), new DateTimeOffset(new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "999988887777", 0, 2, null, null, null, null, new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                column: "last_login_at",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_policies_customer_id",
                table: "policies",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_policies_proposal_id",
                table: "policies",
                column: "proposal_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_customer_id",
                table: "payment_transactions",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_claims_customer_id",
                table: "claims",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_agents_branch_id",
                table: "agents",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "IX_addresses_user_id",
                table: "addresses",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_agent_commissions_agent_id",
                table: "agent_commissions",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "IX_agent_commissions_policy_id",
                table: "agent_commissions",
                column: "policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_agent_commissions_premium_payment_id",
                table: "agent_commissions",
                column: "premium_payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_customer_members_customer_id",
                table: "customer_members",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_customers_user_id",
                table: "customers",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_docreq_key_domain",
                table: "document_requirements",
                columns: new[] { "document_key", "domain" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_logs_user_id",
                table: "email_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_email_templates_key",
                table: "email_templates",
                column: "template_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_endorsements_policy_id",
                table: "endorsements",
                column: "policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_endorsements_requested_by_id",
                table: "endorsements",
                column: "requested_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_endorsements_reviewed_by_id",
                table: "endorsements",
                column: "reviewed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_grievances_assigned_to_id",
                table: "grievances",
                column: "assigned_to_id");

            migrationBuilder.CreateIndex(
                name: "IX_grievances_claim_id",
                table: "grievances",
                column: "claim_id");

            migrationBuilder.CreateIndex(
                name: "IX_grievances_customer_id",
                table: "grievances",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_grievances_policy_id",
                table: "grievances",
                column: "policy_id");

            migrationBuilder.CreateIndex(
                name: "uq_grievances_number",
                table: "grievances",
                column: "grievance_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_kyc_records_reviewed_by_id",
                table: "kyc_records",
                column: "reviewed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_kyc_records_user_id",
                table: "kyc_records",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_kyc_records_id_type_number",
                table: "kyc_records",
                columns: new[] { "id_type", "id_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_nominees_policy_id",
                table: "nominees",
                column: "policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_user_id",
                table: "notifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_premium_rate_tables_product_id",
                table: "premium_rate_tables",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_proposal_members_customer_member_id",
                table: "proposal_members",
                column: "customer_member_id");

            migrationBuilder.CreateIndex(
                name: "IX_proposal_members_proposal_id",
                table: "proposal_members",
                column: "proposal_id");

            migrationBuilder.CreateIndex(
                name: "IX_proposals_agent_id",
                table: "proposals",
                column: "agent_id");

            migrationBuilder.CreateIndex(
                name: "IX_proposals_customer_id",
                table: "proposals",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_proposals_product_id",
                table: "proposals",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_proposals_underwriter_id",
                table: "proposals",
                column: "underwriter_id");

            migrationBuilder.CreateIndex(
                name: "uq_proposals_number",
                table: "proposals",
                column: "proposal_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stripe_customers_user_id",
                table: "stripe_customers",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_stripe_customers_id",
                table: "stripe_customers",
                column: "stripe_customer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_submitted_documents_entity",
                table: "submitted_documents",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_submitted_documents_claim_id",
                table: "submitted_documents",
                column: "claim_id");

            migrationBuilder.CreateIndex(
                name: "IX_submitted_documents_uploaded_by_id",
                table: "submitted_documents",
                column: "uploaded_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_system_configs_updated_by_id",
                table: "system_configs",
                column: "updated_by_id");

            migrationBuilder.CreateIndex(
                name: "uq_system_configs_key",
                table: "system_configs",
                column: "config_key",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_addresses_users_user_id",
                table: "addresses",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_agents_branches_branch_id",
                table: "agents",
                column: "branch_id",
                principalTable: "branches",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_claims_customers_customer_id",
                table: "claims",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_payment_transactions_customers_customer_id",
                table: "payment_transactions",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_policies_customers_customer_id",
                table: "policies",
                column: "customer_id",
                principalTable: "customers",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_policies_proposals_proposal_id",
                table: "policies",
                column: "proposal_id",
                principalTable: "proposals",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_addresses_users_user_id",
                table: "addresses");

            migrationBuilder.DropForeignKey(
                name: "FK_agents_branches_branch_id",
                table: "agents");

            migrationBuilder.DropForeignKey(
                name: "FK_claims_customers_customer_id",
                table: "claims");

            migrationBuilder.DropForeignKey(
                name: "FK_payment_transactions_customers_customer_id",
                table: "payment_transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_policies_customers_customer_id",
                table: "policies");

            migrationBuilder.DropForeignKey(
                name: "FK_policies_proposals_proposal_id",
                table: "policies");

            migrationBuilder.DropTable(
                name: "agent_commissions");

            migrationBuilder.DropTable(
                name: "branches");

            migrationBuilder.DropTable(
                name: "document_requirements");

            migrationBuilder.DropTable(
                name: "email_logs");

            migrationBuilder.DropTable(
                name: "email_templates");

            migrationBuilder.DropTable(
                name: "endorsements");

            migrationBuilder.DropTable(
                name: "grievances");

            migrationBuilder.DropTable(
                name: "kyc_records");

            migrationBuilder.DropTable(
                name: "nominees");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "premium_rate_tables");

            migrationBuilder.DropTable(
                name: "proposal_members");

            migrationBuilder.DropTable(
                name: "stripe_customers");

            migrationBuilder.DropTable(
                name: "submitted_documents");

            migrationBuilder.DropTable(
                name: "system_configs");

            migrationBuilder.DropTable(
                name: "customer_members");

            migrationBuilder.DropTable(
                name: "proposals");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropIndex(
                name: "IX_policies_customer_id",
                table: "policies");

            migrationBuilder.DropIndex(
                name: "IX_policies_proposal_id",
                table: "policies");

            migrationBuilder.DropIndex(
                name: "IX_payment_transactions_customer_id",
                table: "payment_transactions");

            migrationBuilder.DropIndex(
                name: "IX_claims_customer_id",
                table: "claims");

            migrationBuilder.DropIndex(
                name: "IX_agents_branch_id",
                table: "agents");

            migrationBuilder.DropIndex(
                name: "IX_addresses_user_id",
                table: "addresses");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "issued_at",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "policy_type",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "proposal_id",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "payment_transactions");

            migrationBuilder.DropColumn(
                name: "customer_id",
                table: "claims");

            migrationBuilder.DropColumn(
                name: "agent_code",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "agent_type",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "branch_id",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "agents");

            migrationBuilder.DropColumn(
                name: "address_type",
                table: "addresses");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "addresses");

            migrationBuilder.DropColumn(
                name: "is_same_as_permanent",
                table: "addresses");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "addresses");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "addresses");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "users",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "last_login_at",
                table: "users",
                newName: "date_of_birth");

            migrationBuilder.RenameColumn(
                name: "license_expiry",
                table: "agents",
                newName: "license_valid_until");

            migrationBuilder.AddColumn<string>(
                name: "aadhaar_number",
                table: "users",
                type: "character varying(12)",
                maxLength: 12,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "current_address_id",
                table: "users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "gender",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "kyc_status",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "marital_status",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "pan_number",
                table: "users",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "permanent_address_id",
                table: "users",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "document_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    domain = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_sensitive_phi_pii = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    document_type_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    domain = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    file_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    file_path = table.Column<string>(type: "text", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    uploaded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    verification_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_documents", x => x.id);
                    table.CheckConstraint("CK_doc_verification", "verification_status IN ('PENDING', 'VERIFIED', 'REJECTED')");
                    table.ForeignKey(
                        name: "FK_documents_claims_claim_id",
                        column: x => x.claim_id,
                        principalTable: "claims",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_documents_users_reviewed_by_id",
                        column: x => x.reviewed_by_id,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_documents_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "document_types",
                columns: new[] { "id", "code", "domain", "is_sensitive_phi_pii", "name" },
                values: new object[,]
                {
                    { new Guid("10000000-0000-0000-0000-000000000001"), "AADHAAR", "AUTH", true, "Aadhaar Card" },
                    { new Guid("10000000-0000-0000-0000-000000000002"), "PAN", "AUTH", true, "PAN Card" },
                    { new Guid("10000000-0000-0000-0000-000000000003"), "PHOTOGRAPH", "AUTH", false, "Passport Size Photograph" },
                    { new Guid("10000000-0000-0000-0000-000000000004"), "ADDRESS_PROOF", "AUTH", false, "Proof of Address" },
                    { new Guid("10000000-0000-0000-0000-000000000005"), "SUPPORTING_DOC", "HEALTH", true, "Supporting Document" },
                    { new Guid("10000000-0000-0000-0000-000000000006"), "SUPPORTING_DOC", "VEHICLE", false, "Supporting Document" },
                    { new Guid("10000000-0000-0000-0000-000000000007"), "SUPPORTING_DOC", "LIFE", true, "Supporting Document" }
                });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"),
                columns: new[] { "aadhaar_number", "current_address_id", "date_of_birth", "gender", "kyc_status", "marital_status", "pan_number", "permanent_address_id" },
                values: new object[] { "999988887777", new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), new DateTime(1995, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Male", "VERIFIED", "Single", "ABCDE9999F", new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa") });

            migrationBuilder.CreateIndex(
                name: "IX_users_current_address_id",
                table: "users",
                column: "current_address_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_permanent_address_id",
                table: "users",
                column: "permanent_address_id");

            migrationBuilder.CreateIndex(
                name: "uq_users_aadhaar",
                table: "users",
                column: "aadhaar_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_users_pan",
                table: "users",
                column: "pan_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_doctype_code_domain",
                table: "document_types",
                columns: new[] { "code", "domain" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_documents_claim_id",
                table: "documents",
                column: "claim_id");

            migrationBuilder.CreateIndex(
                name: "IX_documents_reviewed_by_id",
                table: "documents",
                column: "reviewed_by_id");

            migrationBuilder.CreateIndex(
                name: "IX_documents_user_id",
                table: "documents",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_users_addresses_current_address_id",
                table: "users",
                column: "current_address_id",
                principalTable: "addresses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_users_addresses_permanent_address_id",
                table: "users",
                column: "permanent_address_id",
                principalTable: "addresses",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
