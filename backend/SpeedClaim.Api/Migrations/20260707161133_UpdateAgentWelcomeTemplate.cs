using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAgentWelcomeTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "email_templates",
                keyColumn: "id",
                keyValue: new System.Guid("dd000018-0000-0000-0000-000000000018"),
                column: "body_html",
                value: "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"UTF-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"><title>Agent Welcome</title></head><body style=\"margin:0;padding:0;background-color:#F7F9FA;font-family:Arial,'Helvetica Neue',Helvetica,sans-serif;\"><table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background-color:#F7F9FA;padding:32px 16px;\"><tr><td align=\"center\"><table role=\"presentation\" width=\"480\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:480px;width:100%;background-color:#ffffff;border-radius:12px;overflow:hidden;\"><tr><td style=\"background:linear-gradient(160deg,#0F6E8C 0%,#0A3040 100%);padding:28px 32px;text-align:center;\"><span style=\"font-size:20px;font-weight:700;color:#ffffff;letter-spacing:-0.3px;\">SpeedClaim</span></td></tr><tr><td style=\"padding:32px;\"><span style=\"display:inline-block;margin:0 0 12px;padding:4px 10px;background:#E8F7F1;color:#1F9D6B;border-radius:20px;font-size:11px;font-weight:700;\">Account Created</span><h1 style=\"margin:0 0 16px;font-size:22px;font-weight:600;color:#1A2230;\">Welcome to SpeedClaim, {{firstName}}!</h1><p style=\"margin:0 0 20px;font-size:15px;line-height:1.6;color:#3C4654;\">Your agent account has been created by an administrator. Set your password below to activate your account and log in.</p><table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse:collapse;border:1px solid #E2E6EB;border-radius:8px;overflow:hidden;\"><tr><td style=\"padding:12px 16px;background:#F7F9FA;font-size:12px;color:#6B7685;width:40%;\">Login email</td><td style=\"padding:12px 16px;font-size:14px;font-weight:700;color:#1A2230;\">{{email}}</td></tr></table><table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" width=\"100%\" style=\"margin-top:24px;\"><tr><td align=\"center\"><table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\"><tr><td style=\"background-color:#0F6E8C;border-radius:8px;\"><a href=\"{{resetUrl}}\" target=\"_blank\" style=\"display:inline-block;padding:14px 36px;font-size:14px;font-weight:600;color:#ffffff;text-decoration:none;\">Set Password &amp; Log In</a></td></tr></table></td></tr></table><p style=\"margin:24px 0 0;font-size:13px;line-height:1.5;color:#6B7685;\">This link expires in 1 hour. Contact your administrator if you have any issues accessing your account.</p></td></tr><tr><td style=\"padding:20px 32px;border-top:1px solid #E2E6EB;text-align:center;\"><p style=\"margin:0;font-size:12px;color:#6B7685;\">&copy; {{year}} SpeedClaim. All rights reserved.</p></td></tr></table></td></tr></table></body></html>");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "email_templates",
                keyColumn: "id",
                keyValue: new System.Guid("dd000018-0000-0000-0000-000000000018"),
                column: "body_html",
                value: "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"UTF-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"><title>Agent Welcome</title></head><body style=\"margin:0;padding:0;background-color:#F7F9FA;font-family:Arial,'Helvetica Neue',Helvetica,sans-serif;\"><table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background-color:#F7F9FA;padding:32px 16px;\"><tr><td align=\"center\"><table role=\"presentation\" width=\"480\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:480px;width:100%;background-color:#ffffff;border-radius:12px;overflow:hidden;\"><tr><td style=\"background:linear-gradient(160deg,#0F6E8C 0%,#0A3040 100%);padding:28px 32px;text-align:center;\"><span style=\"font-size:20px;font-weight:700;color:#ffffff;letter-spacing:-0.3px;\">SpeedClaim</span></td></tr><tr><td style=\"padding:32px;\"><span style=\"display:inline-block;margin:0 0 12px;padding:4px 10px;background:#E8F7F1;color:#1F9D6B;border-radius:20px;font-size:11px;font-weight:700;\">Account Created</span><h1 style=\"margin:0 0 16px;font-size:22px;font-weight:600;color:#1A2230;\">Welcome to SpeedClaim, {{firstName}}!</h1><p style=\"margin:0 0 20px;font-size:15px;line-height:1.6;color:#3C4654;\">Your agent account has been created by an administrator. You can now log in using your registered email address.</p><table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse:collapse;border:1px solid #E2E6EB;border-radius:8px;overflow:hidden;\"><tr><td style=\"padding:12px 16px;background:#F7F9FA;font-size:12px;color:#6B7685;width:40%;\">Login email</td><td style=\"padding:12px 16px;font-size:14px;font-weight:700;color:#1A2230;\">{{email}}</td></tr></table><p style=\"margin:20px 0 0;font-size:13px;line-height:1.5;color:#6B7685;\">Please log in and change your password on first sign-in. Contact your administrator if you have any issues accessing your account.</p></td></tr><tr><td style=\"padding:20px 32px;border-top:1px solid #E2E6EB;text-align:center;\"><p style=\"margin:0;font-size:12px;color:#6B7685;\">&copy; {{year}} SpeedClaim. All rights reserved.</p></td></tr></table></td></tr></table></body></html>");
        }
    }
}
