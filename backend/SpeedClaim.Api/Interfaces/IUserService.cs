using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpeedClaim.Api.Dtos.Auth;
using SpeedClaim.Api.Dtos.User;
using SpeedClaim.Api.Dtos.Common;

namespace SpeedClaim.Api.Interfaces;

public interface IUserService
{
    // Customer
    Task<UserDto> GetProfileAsync(string userId);
    Task UpdateProfileAsync(string userId, UserDto request);
    Task<FamilyMemberDto> AddFamilyMemberAsync(string customerId, AddFamilyMemberRequest request);
    Task UpdateFamilyMemberAsync(string memberId, string customerId, UpdateFamilyMemberRequest request);
    Task<IEnumerable<FamilyMemberDto>> GetFamilyMembersAsync(string customerId);
    Task UploadKycDocumentsAsync(string customerId, KycUploadRequest request);
    Task AddAddressAsync(string userId, SingleAddressRequest request);
    Task UpdateAddressAsync(string addressId, string userId, SingleAddressRequest request);
    Task DeleteAddressAsync(string addressId, string userId);
    Task DeleteFamilyMemberAsync(string memberId, string customerId);

    // Admin / Underwriter
    Task<PagedResponse<UserDto>> GetAllUsersAsync(int page, int pageSize);
    Task<PagedResponse<KycRecordDto>> GetPendingKycAsync(int page, int pageSize);
    Task<KycRecordDto?> GetMyKycAsync(string customerId);
    Task UpdateUserRoleAsync(string targetUserId, string role);
    Task ApproveRejectKycAsync(string customerId, bool isApproved, string reason, string reviewerId);
    Task ActivateDeactivateUserAsync(string targetUserId, bool isActive, string adminId);
    Task<IEnumerable<SessionDto>> GetAllSessionsAsync();
}
