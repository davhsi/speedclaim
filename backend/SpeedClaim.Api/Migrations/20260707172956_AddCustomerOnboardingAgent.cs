using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerOnboardingAgent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "onboarding_agent_id",
                table: "customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "customers",
                keyColumn: "id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"),
                column: "onboarding_agent_id",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_customers_onboarding_agent_id",
                table: "customers",
                column: "onboarding_agent_id");

            migrationBuilder.AddForeignKey(
                name: "FK_customers_agents_onboarding_agent_id",
                table: "customers",
                column: "onboarding_agent_id",
                principalTable: "agents",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_customers_agents_onboarding_agent_id",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_customers_onboarding_agent_id",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "onboarding_agent_id",
                table: "customers");
        }
    }
}
