namespace SpeedClaim.Api.Configuration;

public sealed class AiServiceOptions
{
    public const string SectionName = "AI";

    public string BaseUrl { get; set; } = "http://127.0.0.1:8000";
    public string InternalApiKey { get; set; } = string.Empty;
    public int IngestionTimeoutSeconds { get; set; } = 60;
    public int PolicyQaTimeoutSeconds { get; set; } = 45;
    public int PolicyQaMaxQuestionCharacters { get; set; } = 2000;
    public int BrochureMaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;
}
