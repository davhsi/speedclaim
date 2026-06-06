using System;
using System.Threading.Tasks;
using SpeedClaim.Api.Models;

namespace SpeedClaim.Api.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByEmailWithRolesAsync(string email);
    Task<User?> GetUserWithRefreshTokensAsync(Guid userId);
    Task<bool> IsEmailRegisteredAsync(string email);
    Task<bool> IsAadhaarRegisteredAsync(string aadhaar);
    Task<bool> IsPanRegisteredAsync(string pan);
}
