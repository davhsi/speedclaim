using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_claim_workflow_users_actor_id",
                table: "claim_workflows");

            migrationBuilder.CreateTable(
                name: "payment_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stripe_payment_intent_id = table.Column<string>(type: "text", nullable: false),
                    stripe_event_id = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    currency = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_payment_transactions_policies_policy_id",
                        column: x => x.policy_id,
                        principalTable: "policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_policy_id",
                table: "payment_transactions",
                column: "policy_id");

            migrationBuilder.CreateIndex(
                name: "IX_payment_transactions_stripe_payment_intent_id",
                table: "payment_transactions",
                column: "stripe_payment_intent_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_claim_workflow_users_actor_id",
                table: "claim_workflows",
                column: "actor_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_claim_workflow_users_actor_id",
                table: "claim_workflows");

            migrationBuilder.DropTable(
                name: "payment_transactions");

            migrationBuilder.AddForeignKey(
                name: "FK_claim_workflow_users_actor_id",
                table: "claim_workflows",
                column: "actor_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
