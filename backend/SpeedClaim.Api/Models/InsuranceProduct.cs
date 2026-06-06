using System;

namespace SpeedClaim.Api.Models;

public class InsuranceProduct
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string Domain { get; set; }
    public string Description { get; set; }
    public decimal? MaxCoverage { get; set; }
    public bool IsActive { get; set; } = true;
}
