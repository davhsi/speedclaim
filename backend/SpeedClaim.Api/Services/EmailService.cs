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
            var fromAddress = _configuration["EmailSettings:FromAddress"] ?? "noreply@speedclaim.local";
            email.From.Add(MailboxAddress.Parse(fromAddress));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = body };

            using var smtp = _smtpClientFactory.CreateClient();
            var host = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
            var port = int.TryParse(_configuration["EmailSettings:SmtpPort"], out var p) ? p : 587;
            var user = _configuration["EmailSettings:SmtpUser"] ?? "dummy";
            var pass = _configuration["EmailSettings:SmtpPass"] ?? "dummy";

            await smtp.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls);
            if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
            {
                await smtp.AuthenticateAsync(user, pass);
            }
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {To}", to);

            // Log to database
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
            throw; // Re-throw to inform callers if needed
        }
    }

    public async Task SendEmailVerificationAsync(string to, string token)
    {
        var subject = "Verify Your SpeedClaim Account";
        var body = $"<p>Please verify your account using this token: <strong>{token}</strong></p>";
        await SendEmailAsync(to, subject, body);
    }

    public async Task SendPasswordResetAsync(string to, string token)
    {
        var subject = "Reset Your SpeedClaim Password";
        var body = $"<p>You can reset your password using this token: <strong>{token}</strong></p>";
        await SendEmailAsync(to, subject, body);
    }
}
