using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSpeedyWorkspaceEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "brochure_version",
                table: "speedy_workspace_messages",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "citations_json",
                table: "speedy_workspace_messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "evidence_status",
                table: "speedy_workspace_messages",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "brochure_version",
                table: "speedy_workspace_messages");

            migrationBuilder.DropColumn(
                name: "citations_json",
                table: "speedy_workspace_messages");

            migrationBuilder.DropColumn(
                name: "evidence_status",
                table: "speedy_workspace_messages");
        }
    }
}
