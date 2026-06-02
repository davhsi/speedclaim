using System;
using System.Collections.Generic;

namespace SpeedClaim.Api.Models;

public class PolicyLifeDetail
{
    public Guid PolicyId { get; set; }
    public string NomineeName { get; set; }
    public string NomineeRelation { get; set; }
    public string NomineePhone { get; set; }
    public bool HasAccidentalRider { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Policy Policy { get; set; }
}
