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
    Task UpdateProfileAsync(string userId, UpdateProfileRequest request);
    Task<string> UploadAvatarAsync(string userId, Microsoft.AspNetCore.Http.IFormFile file);
    Task<FamilyMemberDto> AddFamilyMemberAsync(string customerId, AddFamilyMemberRequest request);
    Task UpdateFamilyMemberAsync(string memberId, string customerId, UpdateFamilyMemberRequest request);
    Task<IEnumerable<FamilyMemberDto>> GetFamilyMembersAsync(string customerId);
    Task UploadAadhaarAsync(string customerId, AadhaarUploadRequest request);
    Task UploadPanAsync(string customerId, PanUploadRequest request);
    Task<Guid> AddAddressAsync(string userId, SingleAddressRequest request);
    Task UpdateAddressAsync(string addressId, string userId, SingleAddressRequest request);
    Task DeleteAddressAsync(string addressId, string userId);
    Task DeleteFamilyMemberAsync(string memberId, string customerId);

    // Admin / Underwriter
    Task<PagedResponse<UserDto>> GetAllUsersAsync(int page, int pageSize);
    Task<PagedResponse<KycRecordDto>> GetPendingKycAsync(int page, int pageSize);
    Task<KycRecordDto?> GetMyKycAsync(string customerId);
    Task UpdateUserRoleAsync(string targetUserId, string role, string adminId);
    Task ApproveRejectKycAsync(string customerId, bool isApproved, string reason, string reviewerId);
    Task ActivateDeactivateUserAsync(string targetUserId, bool isActive, string adminId);
    Task<IEnumerable<SessionDto>> GetAllSessionsAsync();
    Task<IEnumerable<SurveyorDto>> GetSurveyorsAsync();
    Task<SurveyorProfileDto> GetSurveyorProfileAsync(string userId);
}
