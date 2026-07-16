using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyAssistantDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "product_brochure_id",
                table: "policies",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "policy_assistant_conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    brochure_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    retain_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_policy_assistant_conversations", x => x.id);
                    table.ForeignKey(
                        name: "FK_policy_assistant_conversations_policies_policy_id",
                        column: x => x.policy_id,
                        principalTable: "policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_policy_assistant_conversations_product_brochures_brochure_id",
                        column: x => x.brochure_id,
                        principalTable: "product_brochures",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_policy_assistant_conversations_users_created_by_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "policy_assistant_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    evidence_status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    citations_json = table.Column<string>(type: "text", nullable: true),
                    model = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    prompt_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_policy_assistant_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_policy_assistant_messages_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "policy_assistant_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_policies_product_brochure_id",
                table: "policies",
                column: "product_brochure_id");

            migrationBuilder.CreateIndex(
                name: "IX_policy_assistant_conversations_brochure_id",
                table: "policy_assistant_conversations",
                column: "brochure_id");

            migrationBuilder.CreateIndex(
                name: "IX_policy_assistant_conversations_created_by_user_id",
                table: "policy_assistant_conversations",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_policy_assistant_conversations_policy_creator_updated",
                table: "policy_assistant_conversations",
                columns: new[] { "policy_id", "created_by_user_id", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_policy_assistant_conversations_retain_until",
                table: "policy_assistant_conversations",
                column: "retain_until");

            migrationBuilder.CreateIndex(
                name: "ix_policy_assistant_messages_conversation_created",
                table: "policy_assistant_messages",
                columns: new[] { "conversation_id", "created_at" });

            migrationBuilder.AddForeignKey(
                name: "FK_policies_product_brochures_product_brochure_id",
                table: "policies",
                column: "product_brochure_id",
                principalTable: "product_brochures",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_policies_product_brochures_product_brochure_id",
                table: "policies");

            migrationBuilder.DropTable(
                name: "policy_assistant_messages");

            migrationBuilder.DropTable(
                name: "policy_assistant_conversations");

            migrationBuilder.DropIndex(
                name: "IX_policies_product_brochure_id",
                table: "policies");

            migrationBuilder.DropColumn(
                name: "product_brochure_id",
                table: "policies");
        }
    }
}
