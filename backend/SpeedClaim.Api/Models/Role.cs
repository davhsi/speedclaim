using System;
using System.Collections.Generic;

namespace SpeedClaim.Api.Models;

public class Role
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Description { get; set; }
    public int HierarchyLevel { get; set; } = 10;
    
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
