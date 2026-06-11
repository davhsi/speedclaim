using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SpeedClaim.Api.Context;
using SpeedClaim.Api.Interfaces;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(SpeedClaimDbContext context) : base(context)
    {
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await Context.Users.SingleOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetUserByEmailWithRolesAsync(string email)
    {
        return await Context.Users

            .SingleOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetUserProfileAsync(Guid userId)
    {
        return await Context.Users

            .Include(u => u.Addresses)
            .Include(u => u.KycRecord)
            .Include(u => u.Customer)
            .SingleOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetUserWithRefreshTokensAsync(Guid userId)
    {
        return await Context.Users
            .Include(u => u.Sessions)
            .SingleOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<bool> IsEmailRegisteredAsync(string email)
    {
        return await Context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task<bool> IsAadhaarRegisteredAsync(string aadhaar)
    {
        return await Context.Users.AnyAsync(u => u.KycRecord != null && u.KycRecord.IdNumber == aadhaar && u.KycRecord.IdType == SpeedClaim.Api.Models.Enums.IdType.Aadhaar);
    }

    public async Task<bool> IsPanRegisteredAsync(string pan)
    {
        return await Context.Users.AnyAsync(u => u.KycRecord != null && u.KycRecord.IdNumber == pan && u.KycRecord.IdType == SpeedClaim.Api.Models.Enums.IdType.Pan);
    }
}
