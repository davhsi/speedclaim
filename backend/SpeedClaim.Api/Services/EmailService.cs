using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;
using SpeedClaim.Api.Contracts;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Services;

public class EmailService : IEmailService
{
    private readonly ISmtpClientFactory _smtpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailDispatchQueue? _emailDispatchQueue;

    public EmailService(
        ISmtpClientFactory smtpClientFactory,
        IConfiguration configuration,
        ILogger<EmailService> logger,
        IUnitOfWork unitOfWork,
        IEmailDispatchQueue? emailDispatchQueue = null)
    {
        _smtpClientFactory = smtpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _unitOfWork = unitOfWork;
        _emailDispatchQueue = emailDispatchQueue;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        await SendEmailCoreAsync(to, subject, body, attachment: null);
    }

    public async Task SendEmailAsync(string to, string subject, string body, EmailAttachment attachment)
    {
        await SendEmailCoreAsync(to, subject, body, attachment);
    }

    private async Task SendEmailCoreAsync(string to, string subject, string body, EmailAttachment? attachment)
    {
        // Production uses Service Bus for ordinary HTML mail. Attachments remain direct because
        // queueing KYC/claim documents would exceed message limits and weaken the trust boundary.
        if (_emailDispatchQueue is not null && attachment is null)
        {
            try
            {
                await _emailDispatchQueue.EnqueueAsync(new EmailDispatchMessage(
                    to,
                    subject,
                    body,
                    DateTimeOffset.UtcNow));

                await WriteEmailLogAsync(to, subject, "Queued", DateTimeOffset.UtcNow);
                _logger.LogInformation("Email queued for asynchronous delivery to {To}", to);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue email for {To}", to);
                await WriteEmailLogAsync(to, subject, "QueueFailed", sentAt: null);
                throw;
            }
        }

        try
        {
            var email = new MimeMessage();
            var senderName = _configuration["SmtpSettings:SenderName"] ?? "SpeedClaim";
            var senderEmail = _configuration["SmtpSettings:SenderEmail"] ?? "noreply@speedclaim.local";
            email.From.Add(new MailboxAddress(senderName, senderEmail));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;

            if (attachment is null)
            {
                email.Body = new TextPart(TextFormat.Html) { Text = body };
            }
            else
            {
                var builder = new BodyBuilder { HtmlBody = body };
                builder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
                email.Body = builder.ToMessageBody();
            }

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

            await WriteEmailLogAsync(to, subject, "Sent", DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}", to);
            await WriteEmailLogAsync(to, subject, "Failed", sentAt: null);
            throw;
        }
    }

    private async Task WriteEmailLogAsync(string to, string subject, string status, DateTimeOffset? sentAt)
    {
        var emailLog = new EmailLog
        {
            Id = Guid.NewGuid(),
            RecipientEmail = to,
            Subject = subject,
            SentAt = sentAt,
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _unitOfWork.EmailLogs.AddAsync(emailLog);
        await _unitOfWork.CompleteAsync();
    }

    public async Task SendTemplatedEmailAsync(string templateKey, Dictionary<string, string> variables, string to, EmailAttachment? attachment = null)
    {
        var (subject, body) = await LoadAndRenderAsync(templateKey, variables);
        await SendEmailCoreAsync(to, subject, body, attachment);
    }

    public async Task SendEmailVerificationAsync(string to, string token)
    {
        var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:4200";
        var verifyUrl = $"{frontendUrl}/auth/verify-email?token={Uri.EscapeDataString(token)}";
        var (subject, body) = await LoadAndRenderAsync("EmailVerification", new Dictionary<string, string>
        {
            ["verifyUrl"] = verifyUrl
        });
        await SendEmailAsync(to, subject, body);
    }

    public async Task SendPasswordResetAsync(string to, string token)
    {
        var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:4200";
        var resetUrl = $"{frontendUrl}/auth/reset-password?token={Uri.EscapeDataString(token)}";
        var (subject, body) = await LoadAndRenderAsync("PasswordReset", new Dictionary<string, string>
        {
            ["resetUrl"] = resetUrl
        });
        await SendEmailAsync(to, subject, body);
    }

    public async Task SendAgentWelcomeAsync(string to, string firstName, string resetToken)
    {
        var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:4200";
        var resetUrl = $"{frontendUrl}/auth/reset-password?token={Uri.EscapeDataString(resetToken)}";
        await SendTemplatedEmailAsync("AgentWelcome", new Dictionary<string, string>
        {
            ["firstName"] = System.Net.WebUtility.HtmlEncode(firstName),
            ["email"] = System.Net.WebUtility.HtmlEncode(to),
            ["resetUrl"] = resetUrl
        }, to);
    }

    public async Task SendCustomerWelcomeAsync(string to, string firstName, string resetToken)
    {
        var frontendUrl = _configuration["FrontendUrl"] ?? "http://localhost:4200";
        var resetUrl = $"{frontendUrl}/auth/reset-password?token={Uri.EscapeDataString(resetToken)}";
        await SendTemplatedEmailAsync("CustomerWelcome", new Dictionary<string, string>
        {
            ["firstName"] = System.Net.WebUtility.HtmlEncode(firstName),
            ["email"] = System.Net.WebUtility.HtmlEncode(to),
            ["resetUrl"] = resetUrl
        }, to);
    }

    // Loads a template from the DB by key, substitutes {{variable}} placeholders, and returns
    // the rendered (subject, body) pair. Always injects {{year}} and {{logoUrl}} automatically.
    // Throws InvalidOperationException if the template key is missing or inactive — this is
    // intentional: a missing template means missing seed data, not a runtime user error.
    private async Task<(string subject, string body)> LoadAndRenderAsync(
        string templateKey, Dictionary<string, string> variables)
    {
        var template = await _unitOfWork.EmailTemplates
            .SingleOrDefaultAsync(t => t.TemplateKey == templateKey && t.IsActive);

        if (template is null)
            throw new InvalidOperationException(
                $"Email template '{templateKey}' is missing or inactive. Run database migrations to seed default templates.");

        variables["year"] = DateTime.UtcNow.Year.ToString();
        // Single hosted logo asset (backend/SpeedClaim.Api/wwwroot/assets/logo.png, served via
        // UseStaticFiles — the same file PolicyDocumentGenerator embeds into certificates) —
        // every template references this one URL instead of embedding its own copy of the logo.
        variables["logoUrl"] = $"{_configuration["ApiBaseUrl"] ?? "http://localhost:5062"}/assets/logo.png";

        var subject = template.Subject;
        var body = template.BodyHtml;
        foreach (var (key, value) in variables)
        {
            subject = subject.Replace("{{" + key + "}}", value);
            body = body.Replace("{{" + key + "}}", value);
        }

        return (subject, body);
    }
}
