using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetByAdminEmailTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "email_templates",
                columns: new[] { "id", "body_html", "created_at", "is_active", "subject", "template_key", "updated_at" },
                values: new object[]
                {
                    new System.Guid("dd000034-0000-0000-0000-000000000034"),
                    "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"UTF-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"><title>Password Reset by Administrator</title></head><body style=\"margin:0;padding:0;background-color:#F7F9FA;font-family:Arial,'Helvetica Neue',Helvetica,sans-serif;\"><table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background-color:#F7F9FA;padding:32px 16px;\"><tr><td align=\"center\"><table role=\"presentation\" width=\"480\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:480px;width:100%;background-color:#ffffff;border-radius:12px;overflow:hidden;\"><tr><td style=\"background:linear-gradient(160deg,#0F6E8C 0%,#0A3040 100%);padding:28px 32px;text-align:center;\"><span style=\"font-size:20px;font-weight:700;color:#ffffff;letter-spacing:-0.3px;\">SpeedClaim</span></td></tr><tr><td style=\"padding:32px;\"><span style=\"display:inline-block;margin:0 0 12px;padding:4px 10px;background:#E6F4F8;color:#0F6E8C;border-radius:20px;font-size:11px;font-weight:700;\">Security Notice</span><h1 style=\"margin:0 0 16px;font-size:22px;font-weight:600;color:#1A2230;\">Your password was reset by an administrator</h1><p style=\"margin:0 0 20px;font-size:15px;line-height:1.6;color:#3C4654;\">Dear {{firstName}}, an administrator has reset the password for your SpeedClaim account ({{email}}). Your new password has not been included in this email — it will be shared with you separately by your administrator.</p><p style=\"margin:0;font-size:13px;line-height:1.5;color:#6B7685;\">If you did not expect this change, please contact SpeedClaim support immediately.</p></td></tr><tr><td style=\"padding:20px 32px;border-top:1px solid #E2E6EB;text-align:center;\"><p style=\"margin:0;font-size:12px;color:#6B7685;\">&copy; {{year}} SpeedClaim. All rights reserved.</p></td></tr></table></td></tr></table></body></html>",
                    new System.DateTimeOffset(new System.DateTime(2026, 7, 9, 0, 0, 0, 0, System.DateTimeKind.Unspecified), new System.TimeSpan(0, 0, 0, 0, 0)),
                    true,
                    "Your SpeedClaim password was reset by an administrator",
                    "PasswordResetByAdmin",
                    null
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "email_templates",
                keyColumn: "id",
                keyValue: new System.Guid("dd000034-0000-0000-0000-000000000034"));
        }
    }
}
