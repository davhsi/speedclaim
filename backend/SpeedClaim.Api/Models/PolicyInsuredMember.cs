using System;
using System.Collections.Generic;

namespace SpeedClaim.Api.Models;

public class PolicyInsuredMember
{
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }
    public string Salutation { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName => $"{Salutation} {FirstName} {LastName}".Trim();
    public DateTime DateOfBirth { get; set; }
    public string RelationToHolder { get; set; }
    public bool IsPrimary { get; set; }
    public Guid AddressId { get; set; }

    // Navigation Properties
    public virtual Address Address { get; set; } = null!;
    public virtual Policy Policy { get; set; }
    public virtual ICollection<ClaimHealthDetail> ClaimHealthDetails { get; set; } = new List<ClaimHealthDetail>();
}
