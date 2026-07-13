using System;

namespace SpeedClaim.Api.Models;

public class InsuranceProduct
{
    public Guid Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string Uin { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public int MinAge { get; set; }
    public int MaxAge { get; set; }
    public decimal MinSumAssured { get; set; }
    public decimal MaxSumAssured { get; set; }
    public int MinTenureYears { get; set; }
    public int MaxTenureYears { get; set; }
    public int WaitingPeriodDays { get; set; }
    public string? MotorVehicleType { get; set; }
    
    public bool AllowsFamilyFloater { get; set; }
    public int MaxFamilyMembers { get; set; }
    
    public bool IsActive { get; set; } = true;
    public bool IsAvailableForSale { get; set; } = true;
    
    public Guid? CreatedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    
    public virtual User? CreatedBy { get; set; }
}
