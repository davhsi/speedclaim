using System.Globalization;
using System.Text;
using System.Threading.Channels;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting.Json;
using SpeedClaim.Api.Configuration;

namespace SpeedClaim.Api.Services;

/// <summary>
/// Buffers Serilog events and persists them as immutable NDJSON block blobs. Block blobs are
/// intentional: Azure Storage lifecycle policies can tier them to cool and archive storage.
/// </summary>
public sealed class BlobLogBatchingSink : ILogEventSink
{
    private readonly Channel<LogEvent> _events;
    private readonly JsonFormatter _formatter = new();
    private readonly BlobContainerClient _container;
    private readonly BlobLogStorageOptions _options;
    private readonly string _instanceName;
    private long _sequence;

    public BlobLogBatchingSink(BlobLogStorageOptions options, string connectionString)
    {
        _options = options;
        _container = new BlobContainerClient(connectionString, options.ContainerName);
        _instanceName = SanitizePathSegment(Environment.MachineName);
        _events = Channel.CreateBounded<LogEvent>(new BoundedChannelOptions(options.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public void Emit(LogEvent logEvent)
    {
        if (!_events.Writer.TryWrite(logEvent))
        {
            SelfLog.WriteLine("SpeedClaim Blob log buffer is full; a log event was dropped.");
        }
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: stoppingToken);
        }
        catch (Exception exception)
        {
            SelfLog.WriteLine("Unable to initialise the SpeedClaim Blob log container: {0}", exception);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var batch = await ReadBatchAsync(stoppingToken);
            if (batch.Count == 0)
            {
                continue;
            }

            await UploadWithRetryAsync(batch, stoppingToken);
        }
    }

    private async Task<List<LogEvent>> ReadBatchAsync(CancellationToken stoppingToken)
    {
        var batch = new List<LogEvent>(_options.BatchSize);
        using var waitTimeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        waitTimeout.CancelAfter(TimeSpan.FromSeconds(_options.FlushPeriodSeconds));

        try
        {
            while (batch.Count < _options.BatchSize
                   && await _events.Reader.WaitToReadAsync(waitTimeout.Token))
            {
                while (batch.Count < _options.BatchSize && _events.Reader.TryRead(out var logEvent))
                {
                    batch.Add(logEvent);
                }
            }
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            // The periodic flush interval elapsed; upload the events already collected.
        }

        return batch;
    }

    private async Task UploadWithRetryAsync(IReadOnlyCollection<LogEvent> batch, CancellationToken stoppingToken)
    {
        var payload = FormatBatch(batch);
        var blobName = BuildBlobName();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _container.GetBlobClient(blobName).UploadAsync(
                    BinaryData.FromBytes(payload),
                    new BlobUploadOptions
                    {
                        HttpHeaders = new BlobHttpHeaders { ContentType = "application/x-ndjson" }
                    },
                    stoppingToken);
                return;
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                SelfLog.WriteLine("Unable to upload SpeedClaim logs to Blob Storage; retrying: {0}", exception);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private byte[] FormatBatch(IEnumerable<LogEvent> batch)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        foreach (var logEvent in batch)
        {
            _formatter.Format(logEvent, writer);
        }

        return Encoding.UTF8.GetBytes(writer.ToString());
    }

    private string BuildBlobName()
    {
        var now = DateTime.UtcNow;
        var prefix = _options.BlobPrefix.Trim('/');
        var sequence = Interlocked.Increment(ref _sequence);
        return $"{prefix}/{now:yyyy/MM/dd}/{_instanceName}/{now:HHmmssfff}-{sequence:D6}.ndjson";
    }

    private static string SanitizePathSegment(string value)
        => new string(value.Select(character => char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '-').ToArray());
}
