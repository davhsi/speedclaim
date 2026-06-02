using System;
using System.Collections.Generic;

namespace SpeedClaim.Api.Models;

public class PolicyHealthDetail
{
    public Guid PolicyId { get; set; }
    public bool CoversDental { get; set; }
    public decimal Deductible { get; set; }
    public string NetworkType { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Policy Policy { get; set; }
}
