using System;
using System.Collections.Generic;

namespace SpeedClaim.Api.Models;

public class PolicyInsuredMember
{
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }
    public string FullName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string RelationToHolder { get; set; }
    public bool IsPrimary { get; set; }

    public Policy Policy { get; set; }
}
