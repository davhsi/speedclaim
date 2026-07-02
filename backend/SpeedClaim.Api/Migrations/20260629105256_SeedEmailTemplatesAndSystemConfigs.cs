using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SpeedClaim.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedEmailTemplatesAndSystemConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "email_templates",
                columns: new[] { "id", "body_html", "created_at", "is_active", "subject", "template_key", "updated_at" },
                values: new object[,]
                {
                    { new Guid("dd000001-0000-0000-0000-000000000001"), "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"UTF-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"><title>Verify your email</title></head><body style=\"margin:0;padding:0;background-color:#F7F9FA;font-family:Arial,'Helvetica Neue',Helvetica,sans-serif;\"><table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background-color:#F7F9FA;padding:32px 16px;\"><tr><td align=\"center\"><table role=\"presentation\" width=\"480\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:480px;width:100%;background-color:#ffffff;border-radius:12px;overflow:hidden;\"><tr><td style=\"background:linear-gradient(160deg,#0F6E8C 0%,#0A3040 100%);padding:28px 32px;text-align:center;\"><span style=\"font-size:20px;font-weight:700;color:#ffffff;letter-spacing:-0.3px;\">SpeedClaim</span></td></tr><tr><td style=\"padding:32px;\"><h1 style=\"margin:0 0 16px;font-size:22px;font-weight:600;color:#1A2230;\">Verify your email</h1><p style=\"margin:0 0 28px;font-size:15px;line-height:1.6;color:#3C4654;\">Welcome to SpeedClaim! Please verify your email address to activate your account and start managing your insurance policies.</p><table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" width=\"100%\"><tr><td align=\"center\"><table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\"><tr><td style=\"background-color:#0F6E8C;border-radius:8px;\"><a href=\"{{verifyUrl}}\" target=\"_blank\" style=\"display:inline-block;padding:14px 36px;font-size:14px;font-weight:600;color:#ffffff;text-decoration:none;\">Verify Email</a></td></tr></table></td></tr></table><p style=\"margin:28px 0 0;font-size:13px;line-height:1.5;color:#6B7685;\">This link expires in 24 hours. If you didn't create an account, you can safely ignore this email.</p></td></tr><tr><td style=\"padding:20px 32px;border-top:1px solid #E2E6EB;text-align:center;\"><p style=\"margin:0;font-size:12px;color:#6B7685;\">&copy; {{year}} SpeedClaim. All rights reserved.</p></td></tr></table></td></tr></table></body></html>", new DateTimeOffset(new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "Verify Your SpeedClaim Account", "EmailVerification", null },
                    { new Guid("dd000002-0000-0000-0000-000000000002"), "<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"UTF-8\"><meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\"><title>Reset your password</title></head><body style=\"margin:0;padding:0;background-color:#F7F9FA;font-family:Arial,'Helvetica Neue',Helvetica,sans-serif;\"><table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background-color:#F7F9FA;padding:32px 16px;\"><tr><td align=\"center\"><table role=\"presentation\" width=\"480\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:480px;width:100%;background-color:#ffffff;border-radius:12px;overflow:hidden;\"><tr><td style=\"background:linear-gradient(160deg,#0F6E8C 0%,#0A3040 100%);padding:28px 32px;text-align:center;\"><span style=\"font-size:20px;font-weight:700;color:#ffffff;letter-spacing:-0.3px;\">SpeedClaim</span></td></tr><tr><td style=\"padding:32px;\"><h1 style=\"margin:0 0 16px;font-size:22px;font-weight:600;color:#1A2230;\">Reset your password</h1><p style=\"margin:0 0 28px;font-size:15px;line-height:1.6;color:#3C4654;\">We received a request to reset your password. Click the button below to choose a new one.</p><table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\" width=\"100%\"><tr><td align=\"center\"><table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\"><tr><td style=\"background-color:#0F6E8C;border-radius:8px;\"><a href=\"{{resetUrl}}\" target=\"_blank\" style=\"display:inline-block;padding:14px 36px;font-size:14px;font-weight:600;color:#ffffff;text-decoration:none;\">Reset Password</a></td></tr></table></td></tr></table><p style=\"margin:28px 0 0;font-size:13px;line-height:1.5;color:#6B7685;\">This link expires in 1 hour. If you didn't request a password reset, you can safely ignore this email.</p></td></tr><tr><td style=\"padding:20px 32px;border-top:1px solid #E2E6EB;text-align:center;\"><p style=\"margin:0;font-size:12px;color:#6B7685;\">&copy; {{year}} SpeedClaim. All rights reserved.</p></td></tr></table></td></tr></table></body></html>", new DateTimeOffset(new DateTime(2026, 6, 7, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), true, "Reset Your SpeedClaim Password", "PasswordReset", null }
                });

            migrationBuilder.InsertData(
                table: "system_configs",
                columns: new[] { "id", "config_key", "config_value", "description", "updated_at", "updated_by_id" },
                values: new object[,]
                {
                    { new Guid("cc000001-0000-0000-0000-000000000001"), "AllowCustomerRegistration", "true", "Allow new customers to self-register via the portal. Set to false to disable public sign-up.", null, null },
                    { new Guid("cc000002-0000-0000-0000-000000000002"), "PremiumGracePeriodDays", "30", "Number of days after the due date before a policy lapses for non-payment.", null, null },
                    { new Guid("cc000003-0000-0000-0000-000000000003"), "ClaimApprovalThresholdInr", "100000", "Claims above this amount (INR) require senior underwriter sign-off before approval.", null, null },
                    { new Guid("cc000004-0000-0000-0000-000000000004"), "KycVerificationRequired", "true", "Require completed KYC verification before a policy can be issued to a customer.", null, null },
                    { new Guid("cc000005-0000-0000-0000-000000000005"), "MaxPremiumPaymentRetries", "3", "Maximum number of times a failed premium payment can be retried before the schedule is flagged.", null, null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "email_templates",
                keyColumn: "id",
                keyValue: new Guid("dd000001-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "email_templates",
                keyColumn: "id",
                keyValue: new Guid("dd000002-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "system_configs",
                keyColumn: "id",
                keyValue: new Guid("cc000001-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "system_configs",
                keyColumn: "id",
                keyValue: new Guid("cc000002-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "system_configs",
                keyColumn: "id",
                keyValue: new Guid("cc000003-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "system_configs",
                keyColumn: "id",
                keyValue: new Guid("cc000004-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "system_configs",
                keyColumn: "id",
                keyValue: new Guid("cc000005-0000-0000-0000-000000000005"));
        }
    }
}
