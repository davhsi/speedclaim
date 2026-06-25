using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Services;

public class EmailService : IEmailService
{
    private readonly ISmtpClientFactory _smtpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public EmailService(
        ISmtpClientFactory smtpClientFactory,
        IConfiguration configuration,
        ILogger<EmailService> logger,
        IUnitOfWork unitOfWork)
    {
        _smtpClientFactory = smtpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var email = new MimeMessage();
            var senderName = _configuration["SmtpSettings:SenderName"] ?? "SpeedClaim";
            var senderEmail = _configuration["SmtpSettings:SenderEmail"] ?? "noreply@speedclaim.local";
            email.From.Add(new MailboxAddress(senderName, senderEmail));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = body };

            using var smtp = _smtpClientFactory.CreateClient();
            var host = _configuration["SmtpSettings:Host"] ?? "smtp.gmail.com";
            var port = int.TryParse(_configuration["SmtpSettings:Port"], out var p) ? p : 587;
            var user = _configuration["SmtpSettings:SenderEmail"] ?? "";
            var pass = _configuration["SmtpSettings:AppPassword"] ?? "";

            await smtp.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
            if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
            {
                await smtp.AuthenticateAsync(user, pass);
            }
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {To}", to);

            var emailLog = new EmailLog
            {
                Id = Guid.NewGuid(),
                RecipientEmail = to,
                Subject = subject,
                SentAt = DateTimeOffset.UtcNow,
                Status = "Sent",
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _unitOfWork.EmailLogs.AddAsync(emailLog);
            await _unitOfWork.CompleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            var emailLog = new EmailLog
            {
                Id = Guid.NewGuid(),
                RecipientEmail = to,
                Subject = subject,
                SentAt = null,
                Status = "Failed",
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _unitOfWork.EmailLogs.AddAsync(emailLog);
            await _unitOfWork.CompleteAsync();
            throw;
        }
    }

    public async Task SendEmailVerificationAsync(string to, string token)
    {
        var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:4200";
        var verifyUrl = $"{frontendUrl}/auth/verify-email?token={Uri.EscapeDataString(token)}";

        var subject = "Verify Your SpeedClaim Account";
        var body = BuildEmailTemplate(
            heading: "Verify your email",
            message: "Welcome to SpeedClaim! Please verify your email address to activate your account and start managing your insurance policies.",
            buttonText: "Verify Email",
            buttonUrl: verifyUrl,
            footnote: "This link expires in 24 hours. If you didn't create an account, you can safely ignore this email."
        );
        await SendEmailAsync(to, subject, body);
    }

    public async Task SendPasswordResetAsync(string to, string token)
    {
        var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:4200";
        var resetUrl = $"{frontendUrl}/auth/reset-password?token={Uri.EscapeDataString(token)}";

        var subject = "Reset Your SpeedClaim Password";
        var body = BuildEmailTemplate(
            heading: "Reset your password",
            message: "We received a request to reset your password. Click the button below to choose a new one.",
            buttonText: "Reset Password",
            buttonUrl: resetUrl,
            footnote: "This link expires in 1 hour. If you didn't request a password reset, you can safely ignore this email."
        );
        await SendEmailAsync(to, subject, body);
    }

    private static string BuildEmailTemplate(string heading, string message, string buttonText, string buttonUrl, string footnote)
    {
        var year = DateTime.UtcNow.Year;
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{heading}</title>
</head>
<body style=""margin:0; padding:0; background-color:#F7F9FA; font-family:Arial, 'Helvetica Neue', Helvetica, sans-serif;"">
    <table role=""presentation"" width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color:#F7F9FA; padding:32px 16px;"">
        <tr>
            <td align=""center"">
                <table role=""presentation"" width=""480"" cellpadding=""0"" cellspacing=""0"" style=""max-width:480px; width:100%; background-color:#ffffff; border-radius:12px; overflow:hidden;"">
                    <tr>
                        <td style=""background:linear-gradient(160deg, #0F6E8C 0%, #0A3040 100%); padding:28px 32px; text-align:center;"">
                            <span style=""font-size:20px; font-weight:700; color:#ffffff; letter-spacing:-0.3px;"">SpeedClaim</span>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding:32px;"">
                            <h1 style=""margin:0 0 16px; font-size:22px; font-weight:600; color:#1A2230;"">{heading}</h1>
                            <p style=""margin:0 0 28px; font-size:15px; line-height:1.6; color:#3C4654;"">{message}</p>
                            <table role=""presentation"" cellpadding=""0"" cellspacing=""0"" width=""100%"">
                                <tr>
                                    <td align=""center"">
                                        <table role=""presentation"" cellpadding=""0"" cellspacing=""0"">
                                            <tr>
                                                <td style=""background-color:#0F6E8C; border-radius:8px;"">
                                                    <a href=""{buttonUrl}"" target=""_blank"" style=""display:inline-block; padding:14px 36px; font-size:14px; font-weight:600; color:#ffffff; text-decoration:none;"">{buttonText}</a>
                                                </td>
                                            </tr>
                                        </table>
                                    </td>
                                </tr>
                            </table>
                            <p style=""margin:28px 0 0; font-size:13px; line-height:1.5; color:#6B7685;"">{footnote}</p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding:20px 32px; border-top:1px solid #E2E6EB; text-align:center;"">
                            <p style=""margin:0; font-size:12px; color:#6B7685;"">&copy; {year} SpeedClaim. All rights reserved.</p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }
}
