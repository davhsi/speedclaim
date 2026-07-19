using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSpeedyWorkspaceConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "speedy_workspace_conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    retain_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_speedy_workspace_conversations", x => x.id);
                    table.ForeignKey(
                        name: "FK_speedy_workspace_conversations_users_created_by_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "speedy_workspace_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    content = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    intent = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    risk = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    actions_json = table.Column<string>(type: "text", nullable: true),
                    model = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_speedy_workspace_messages", x => x.id);
                    table.ForeignKey(
                        name: "FK_speedy_workspace_messages_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "speedy_workspace_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_speedy_workspace_conversations_creator_updated",
                table: "speedy_workspace_conversations",
                columns: new[] { "created_by_user_id", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_speedy_workspace_conversations_retain_until",
                table: "speedy_workspace_conversations",
                column: "retain_until");

            migrationBuilder.CreateIndex(
                name: "ix_speedy_workspace_messages_conversation_created",
                table: "speedy_workspace_messages",
                columns: new[] { "conversation_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "speedy_workspace_messages");

            migrationBuilder.DropTable(
                name: "speedy_workspace_conversations");
        }
    }
}
