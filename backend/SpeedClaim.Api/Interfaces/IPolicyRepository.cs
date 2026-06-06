using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Interfaces;

public interface IPolicyRepository : IRepository<Policy>
{
    Task<VehiclePolicy?> GetVehiclePolicyByIdAsync(Guid id);
    Task<HealthPolicy?> GetHealthPolicyByIdAsync(Guid id);
    Task<LifePolicy?> GetLifePolicyByIdAsync(Guid id);
}
