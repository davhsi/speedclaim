using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Interfaces;

public interface IPolicyRepository : IRepository<Policy>
{
    Task<Policy?> GetMotorPolicyByIdAsync(Guid id);
    Task<Policy?> GetHealthPolicyByIdAsync(Guid id);
    Task<Policy?> GetLifePolicyByIdAsync(Guid id);
}
