using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Interfaces;

public interface IClaimRepository : IRepository<Claim>
{
    Task<Claim?> GetClaimWithDetailsAsync(Guid claimId);
}
