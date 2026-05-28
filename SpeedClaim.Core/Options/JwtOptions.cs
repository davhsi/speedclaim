namespace SpeedClaim.Core.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    
    public string Secret { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int ExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
