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
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetUserWithRefreshTokensAsync(Guid userId)
    {
        return await Context.Users
            .Include(u => u.RefreshTokens)
            .SingleOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<bool> IsEmailRegisteredAsync(string email)
    {
        return await Context.Users.AnyAsync(u => u.Email == email);
    }

    public async Task<bool> IsAadhaarRegisteredAsync(string aadhaar)
    {
        return await Context.Users.AnyAsync(u => u.AadhaarNumber == aadhaar);
    }

    public async Task<bool> IsPanRegisteredAsync(string pan)
    {
        return await Context.Users.AnyAsync(u => u.PanNumber == pan);
    }
}
