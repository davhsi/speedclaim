using System;

namespace SpeedClaim.Api.Models;

public class DocumentRequirement
{
    public Guid Id { get; set; }

    public Guid? ProductId { get; set; }
    public InsuranceProduct? Product { get; set; }

    public SpeedClaim.Api.Models.Enums.EntityType EntityType { get; set; }
    public string Domain { get; set; } = "ALL";

    public string DocumentKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public bool IsMandatory { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
