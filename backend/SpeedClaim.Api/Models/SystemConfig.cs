using System;

namespace SpeedClaim.Api.Models;

public class SystemConfig
{
    public Guid Id { get; set; }
    
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public Guid? UpdatedById { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public virtual User? UpdatedBy { get; set; }
}
