using System;
using System.Collections.Generic;

namespace SpeedClaim.Api.Models;

public class PolicyVehicleDetail
{
    public Guid PolicyId { get; set; }
    public string VehicleNumber { get; set; }
    public string Make { get; set; }
    public string Model { get; set; }
    public int ManufactureYear { get; set; }
    public decimal InsuredDeclaredValue { get; set; }
    public bool IsComprehensive { get; set; } = true;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Policy Policy { get; set; }
}
