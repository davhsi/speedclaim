using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;
using SpeedClaim.Api.Interfaces;

namespace SpeedClaim.Api.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        try
        {
            var host = _configuration.GetSection("SmtpSettings")["Host"] ?? "smtp.gmail.com";
            var port = int.Parse(_configuration.GetSection("SmtpSettings")["Port"] ?? "587");
            var senderName = _configuration.GetSection("SmtpSettings")["SenderName"] ?? "SpeedClaim";
            var senderEmail = _configuration.GetSection("SmtpSettings")["SenderEmail"] ?? "noreply@speedclaim.com";
            var appPassword = _configuration.GetSection("SmtpSettings")["AppPassword"];

            // If dummy password is still there, skip actual sending to avoid exceptions breaking the flow
            if (string.IsNullOrEmpty(appPassword) || appPassword == "dummy_password")
            {
                _logger.LogWarning("Email sending skipped: Dummy or missing AppPassword. Target: {ToEmail}, Subject: {Subject}", toEmail, subject);
                return;
            }

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(senderName, senderEmail));
            email.To.Add(new MailboxAddress(toName, toEmail));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(senderEmail, appPassword);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
            
            _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
        }
    }
}
