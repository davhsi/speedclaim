using System;

namespace SpeedClaim.Api.Models;

public class DocumentType
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsSensitivePhiPii { get; set; } = false;
}
