using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MimeKit.Text;
using SpeedClaim.Notifications.Functions.Contracts;

namespace SpeedClaim.Notifications.Functions.Services;

public sealed class SmtpEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(EmailDispatchMessage message, CancellationToken cancellationToken)
    {
        var host = Require("SmtpSettings:Host");
        var senderEmail = Require("SmtpSettings:SenderEmail");
        var appPassword = Require("SmtpSettings:AppPassword");
        var senderName = _configuration["SmtpSettings:SenderName"] ?? "SpeedClaim";
        var port = int.TryParse(_configuration["SmtpSettings:Port"], out var configuredPort) ? configuredPort : 587;

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(senderName, senderEmail));
        email.To.Add(MailboxAddress.Parse(message.To));
        email.Subject = message.Subject;
        email.Body = new TextPart(TextFormat.Html) { Text = message.HtmlBody };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls, cancellationToken);
        await smtp.AuthenticateAsync(senderEmail, appPassword, cancellationToken);
        await smtp.SendAsync(email, cancellationToken);
        await smtp.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation("Queued email delivered to {Recipient}", message.To);
    }

    private string Require(string key) => _configuration[key]
        ?? throw new InvalidOperationException($"Required configuration '{key}' is missing.");
}
