using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SpeedClaim.Notifications.Functions.Contracts;
using SpeedClaim.Notifications.Functions.Services;

namespace SpeedClaim.Notifications.Functions.Functions;

public sealed class EmailDispatchFunction
{
    private readonly SmtpEmailSender _emailSender;
    private readonly ILogger<EmailDispatchFunction> _logger;

    public EmailDispatchFunction(SmtpEmailSender emailSender, ILogger<EmailDispatchFunction> logger)
    {
        _emailSender = emailSender;
        _logger = logger;
    }

    [Function(nameof(EmailDispatchFunction))]
    public async Task Run(
        [ServiceBusTrigger("%ServiceBusQueueName%", Connection = "ServiceBusConnection")]
        string payload,
        CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<EmailDispatchMessage>(payload)
            ?? throw new InvalidOperationException("Email dispatch message was empty or malformed.");

        if (string.IsNullOrWhiteSpace(message.To)
            || string.IsNullOrWhiteSpace(message.Subject)
            || string.IsNullOrWhiteSpace(message.HtmlBody))
        {
            throw new InvalidOperationException("Email dispatch message was missing required fields.");
        }

        _logger.LogInformation("Delivering queued email to {Recipient}", message.To);
        await _emailSender.SendAsync(message, cancellationToken);
    }
}
