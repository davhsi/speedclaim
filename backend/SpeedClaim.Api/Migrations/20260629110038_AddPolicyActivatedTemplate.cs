using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyActivatedTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "email_templates",
                columns: new[] { "id", "body_html", "created_at", "is_active", "subject", "template_key", "updated_at" },
                values: new object[] { new Guid("dd000003-0000-0000-0000-000000000003"), "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"UTF-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"><title>Policy Activated</title></head><body style=\"margin:0;padding:0;background-color:#F4F7FA;font-family:Arial,Helvetica,sans-serif;color:#1A2230;\"><table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background-color:#F4F7FA;padding:28px 12px;\"><tr><td align=\"center\"><table role=\"presentation\" width=\"640\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:640px;width:100%;background:#ffffff;border:1px solid #E2E6EB;border-radius:14px;overflow:hidden;\"><tr><td style=\"background:#0F6E8C;padding:24px 30px;\"><div style=\"font-size:22px;font-weight:700;color:#ffffff;\">SpeedClaim</div><div style=\"font-size:13px;color:#D8EEF5;margin-top:4px;\">Insurance policy services</div></td></tr><tr><td style=\"padding:30px;\"><p style=\"margin:0 0 10px;font-size:14px;color:#1F9D6B;font-weight:700;\">Policy activated</p><h1 style=\"margin:0 0 14px;font-size:25px;line-height:1.25;color:#1A2230;\">Your cover is now active</h1><p style=\"margin:0 0 24px;font-size:15px;line-height:1.65;color:#3C4654;\">Dear {{firstName}}, your policy has been activated successfully. A PDF copy of your policy certificate is attached to this email for your records.</p><table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"border-collapse:collapse;border:1px solid #E2E6EB;border-radius:10px;overflow:hidden;\"><tr><td style=\"padding:13px 16px;background:#F7F9FA;font-size:12px;color:#6B7685;width:38%;\">Policy number</td><td style=\"padding:13px 16px;font-size:14px;font-weight:700;color:#1A2230;\">{{policyNumber}}</td></tr><tr><td style=\"padding:13px 16px;background:#F7F9FA;font-size:12px;color:#6B7685;\">Product</td><td style=\"padding:13px 16px;font-size:14px;color:#1A2230;\">{{product}}</td></tr><tr><td style=\"padding:13px 16px;background:#F7F9FA;font-size:12px;color:#6B7685;\">Coverage amount</td><td style=\"padding:13px 16px;font-size:14px;color:#1A2230;\">{{sumAssured}} INR</td></tr><tr><td style=\"padding:13px 16px;background:#F7F9FA;font-size:12px;color:#6B7685;\">Premium</td><td style=\"padding:13px 16px;font-size:14px;color:#1A2230;\">{{premiumAmount}} INR / {{frequency}}</td></tr><tr><td style=\"padding:13px 16px;background:#F7F9FA;font-size:12px;color:#6B7685;\">Policy period</td><td style=\"padding:13px 16px;font-size:14px;color:#1A2230;\">{{startDate}} - {{endDate}}</td></tr><tr><td style=\"padding:13px 16px;background:#F7F9FA;font-size:12px;color:#6B7685;\">Status</td><td style=\"padding:13px 16px;font-size:14px;font-weight:700;color:#1F9D6B;\">{{status}}</td></tr></table><p style=\"margin:24px 0 0;font-size:13px;line-height:1.55;color:#6B7685;\">Please review the attached certificate and keep it with your insurance records. Contact SpeedClaim support if any detail looks incorrect.</p></td></tr><tr><td style=\"padding:18px 30px;background:#F7F9FA;border-top:1px solid #E2E6EB;\"><p style=\"margin:0;font-size:12px;line-height:1.5;color:#6B7685;\">This is a system-generated message from SpeedClaim. &copy; {{year}} SpeedClaim.</p></td></tr></table></td></tr></table></body></html>", new DateTimeOffset(new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "Your SpeedClaim policy is active - {{policyNumber}}", "PolicyActivated", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "email_templates",
                keyColumn: "id",
                keyValue: new Guid("dd000003-0000-0000-0000-000000000003"));
        }
    }
}
