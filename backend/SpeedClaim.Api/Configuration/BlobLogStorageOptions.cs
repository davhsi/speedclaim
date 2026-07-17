namespace SpeedClaim.Api.Configuration;

public sealed class BlobLogStorageOptions
{
    public const string SectionName = "Logging:BlobStorage";

    public bool Enabled { get; init; }
    public string? ConnectionString { get; init; }
    public string ContainerName { get; init; } = "speedclaim-logs";
    public string BlobPrefix { get; init; } = "api";
    public int BatchSize { get; init; } = 200;
    public int FlushPeriodSeconds { get; init; } = 10;
    public int QueueCapacity { get; init; } = 10_000;
}
