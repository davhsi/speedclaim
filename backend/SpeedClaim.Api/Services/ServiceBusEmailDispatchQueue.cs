using System.Text.Json;
using Azure.Messaging.ServiceBus;
using SpeedClaim.Api.Contracts;
using SpeedClaim.Api.Interfaces;

namespace SpeedClaim.Api.Services;

public sealed class ServiceBusEmailDispatchQueue : IEmailDispatchQueue, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    public ServiceBusEmailDispatchQueue(IConfiguration configuration)
    {
        var connectionString = configuration["EmailDelivery:ServiceBusConnectionString"];
        var queueName = configuration["EmailDelivery:QueueName"];

        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(queueName))
            throw new InvalidOperationException(
                "EmailDelivery:ServiceBusConnectionString and EmailDelivery:QueueName are required when Service Bus delivery is enabled.");

        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(queueName);
    }

    public async Task EnqueueAsync(EmailDispatchMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(message);
        var serviceBusMessage = new ServiceBusMessage(payload)
        {
            ContentType = "application/json",
            Subject = "email-dispatch",
            MessageId = Guid.NewGuid().ToString("N")
        };

        await _sender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _sender.DisposeAsync();
        await _client.DisposeAsync();
    }
}
