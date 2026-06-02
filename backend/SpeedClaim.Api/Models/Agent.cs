using System;
using System.Collections.Generic;

namespace SpeedClaim.Api.Models;

public class Agent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string LicenseNumber { get; set; }
    public string AgencyName { get; set; }
    public DateTime LicenseValidUntil { get; set; }
    public decimal CommissionRate { get; set; } = 0.0000m;
    public bool IsActive { get; set; } = true;

    public User User { get; set; }
}
