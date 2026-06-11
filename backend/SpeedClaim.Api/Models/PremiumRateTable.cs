using System;

namespace SpeedClaim.Api.Models;

public class PremiumRateTable
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    
    public int AgeMin { get; set; }
    public int AgeMax { get; set; }
    public decimal SumAssuredMin { get; set; }
    public decimal SumAssuredMax { get; set; }
    public decimal AnnualPremium { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public virtual InsuranceProduct Product { get; set; } = null!;
}
