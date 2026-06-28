using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedClaim.Api.Dtos.Auth;
using SpeedClaim.Api.Dtos.Common;
using SpeedClaim.Api.Dtos.SystemManagement;
using SpeedClaim.Api.Dtos.User;
using SpeedClaim.Api.Interfaces;
using System.Security.Claims;

namespace SpeedClaim.Api.Controllers;

public class UsersController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly IAgentService _agentService;
    private readonly INotificationService _notificationService;

    public UsersController(IUserService userService, IAgentService agentService, INotificationService notificationService)
    {
        _userService = userService;
        _agentService = agentService;
        _notificationService = notificationService;
    }

    #region Profile

    /// <summary>Get the full profile of the authenticated customer including addresses</summary>
    [Authorize(Roles = "Customer")]
    [HttpGet("profile")]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _userService.GetProfileAsync(userId);
        return Ok(result);
    }

    /// <summary>Update the authenticated user's profile (name, phone, marital status, salutation)</summary>
    [Authorize]
    [HttpPut("profile")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateProfile([FromBody] UserDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _userService.UpdateProfileAsync(userId, request);
        return Ok(new { message = "Profile updated successfully." });
    }

    #endregion

    #region Family Members

    /// <summary>Add a family member (spouse, child, parent) to the customer's profile for floater policies</summary>
    [Authorize(Roles = "Customer")]
    [HttpPost("family")]
    [ProducesResponseType(typeof(FamilyMemberDto), 200)]
    public async Task<IActionResult> AddFamilyMember([FromBody] AddFamilyMemberRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _userService.AddFamilyMemberAsync(userId, request);
        return Ok(result);
    }

    /// <summary>Get all family members linked to the authenticated customer</summary>
    [Authorize(Roles = "Customer")]
    [HttpGet("family")]
    [ProducesResponseType(typeof(IEnumerable<FamilyMemberDto>), 200)]
    public async Task<IActionResult> GetFamilyMembers()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _userService.GetFamilyMembersAsync(userId);
        return Ok(result);
    }

    /// <summary>Update an existing family member's details</summary>
    /// <param name="memberId">Family member ID</param>
    [Authorize(Roles = "Customer")]
    [HttpPut("family/{memberId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateFamilyMember(string memberId, [FromBody] UpdateFamilyMemberRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _userService.UpdateFamilyMemberAsync(memberId, userId, request);
        return Ok(new { message = "Family member updated successfully." });
    }

    /// <summary>Remove a family member from the customer's profile</summary>
    /// <param name="memberId">Family member ID</param>
    [Authorize(Roles = "Customer")]
    [HttpDelete("family/{memberId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteFamilyMember(string memberId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _userService.DeleteFamilyMemberAsync(memberId, userId);
        return Ok(new { message = "Family member removed successfully." });
    }

    #endregion

    #region KYC

    /// <summary>Get the KYC record for the authenticated customer</summary>
    [Authorize(Roles = "Customer")]
    [HttpGet("kyc")]
    [ProducesResponseType(typeof(KycRecordDto), 200)]
    public async Task<IActionResult> GetMyKyc()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var kycRecord = await _userService.GetMyKycAsync(userId);
        return Ok(kycRecord);
    }

    /// <summary>Upload Aadhaar identity document (front and optional back). Agents can upload on behalf of a customer.</summary>
    [Authorize(Roles = "Customer,Agent")]
    [HttpPost("kyc/aadhaar")]
    [ProducesResponseType(typeof(KycRecordDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UploadAadhaar([FromForm] AadhaarUploadRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (role == "Agent" && request.CustomerId.HasValue)
        {
            await _agentService.EnsureCustomerAssignedAsync(userId, request.CustomerId.Value.ToString());
            userId = request.CustomerId.Value.ToString();
        }

        await _userService.UploadAadhaarAsync(userId, request);
        var kyc = await _userService.GetMyKycAsync(userId);
        return Ok(kyc);
    }

    /// <summary>Upload PAN identity document (front and optional back). Agents can upload on behalf of a customer.</summary>
    [Authorize(Roles = "Customer,Agent")]
    [HttpPost("kyc/pan")]
    [ProducesResponseType(typeof(KycRecordDto), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UploadPan([FromForm] PanUploadRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (role == "Agent" && request.CustomerId.HasValue)
        {
            await _agentService.EnsureCustomerAssignedAsync(userId, request.CustomerId.Value.ToString());
            userId = request.CustomerId.Value.ToString();
        }

        await _userService.UploadPanAsync(userId, request);
        var kyc = await _userService.GetMyKycAsync(userId);
        return Ok(kyc);
    }

    #endregion

    #region Addresses

    /// <summary>Add a new address (Permanent or Current) to the customer's profile</summary>
    [Authorize(Roles = "Customer")]
    [HttpPost("addresses")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> AddAddress([FromBody] SingleAddressRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var id = await _userService.AddAddressAsync(userId, request);
        return Ok(new { id, message = "Address added successfully" });
    }

    /// <summary>Update an existing address</summary>
    /// <param name="addressId">Address ID</param>
    [Authorize(Roles = "Customer")]
    [HttpPut("addresses/{addressId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateAddress(string addressId, [FromBody] SingleAddressRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _userService.UpdateAddressAsync(addressId, userId, request);
        return Ok(new { message = "Address updated successfully." });
    }

    /// <summary>Delete an address from the customer's profile</summary>
    /// <param name="addressId">Address ID</param>
    [Authorize(Roles = "Customer")]
    [HttpDelete("addresses/{addressId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteAddress(string addressId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _userService.DeleteAddressAsync(addressId, userId);
        return Ok(new { message = "Address removed successfully." });
    }

    #endregion

    #region Notifications

    /// <summary>Get all in-app notifications for the authenticated user, ordered newest first</summary>
    [Authorize]
    [HttpGet("notifications")]
    [ProducesResponseType(typeof(IEnumerable<NotificationDto>), 200)]
    public async Task<IActionResult> GetNotifications()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        var result = await _notificationService.GetForUserAsync(userId);
        return Ok(result);
    }

    /// <summary>Mark a single notification as read</summary>
    /// <param name="id">Notification ID</param>
    [Authorize]
    [HttpPatch("notifications/{id}/read")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> MarkNotificationRead(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        await _notificationService.MarkAsReadAsync(id, userId);
        return Ok(new { message = "Notification marked as read." });
    }

    /// <summary>Mark all unread notifications as read for the authenticated user</summary>
    [Authorize]
    [HttpPatch("notifications/read-all")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> MarkAllNotificationsRead()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(new { message = "All notifications marked as read." });
    }

    #endregion

    #region Surveyors

    /// <summary>Get all active surveyors for assignment</summary>
    [Authorize(Roles = "ClaimsOfficer,Admin")]
    [HttpGet("surveyors")]
    [ProducesResponseType(typeof(IEnumerable<SurveyorDto>), 200)]
    public async Task<IActionResult> GetSurveyors()
    {
        var result = await _userService.GetSurveyorsAsync();
        return Ok(result);
    }

    /// <summary>Get the authenticated surveyor's profile and license metadata</summary>
    [Authorize(Roles = "Surveyor")]
    [HttpGet("surveyor/profile")]
    [ProducesResponseType(typeof(SurveyorProfileDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMySurveyorProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _userService.GetSurveyorProfileAsync(userId);
        return Ok(result);
    }

    #endregion

    #region Underwriter / Admin — KYC Review

    /// <summary>Get all customer KYC submissions currently in Pending status awaiting review</summary>
    [Authorize(Roles = "Underwriter,Admin")]
    [HttpGet("kyc/pending")]
    [ProducesResponseType(typeof(PagedResponse<KycRecordDto>), 200)]
    public async Task<IActionResult> GetPendingKyc([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _userService.GetPendingKycAsync(page, pageSize);
        return Ok(result);
    }

    /// <summary>Approve or reject a customer's KYC submission. Both Aadhaar and PAN must be uploaded before approval.</summary>
    /// <param name="customerId">Customer user ID</param>
    [Authorize(Roles = "Underwriter,Admin")]
    [HttpPut("{customerId}/kyc/review")]
    [ProducesResponseType(typeof(KycRecordDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ApproveRejectKyc(string customerId, [FromQuery] bool isApproved, [FromQuery] string reason)
    {
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _userService.ApproveRejectKycAsync(customerId, isApproved, reason, reviewerId);
        var kyc = await _userService.GetMyKycAsync(customerId);
        return Ok(kyc);
    }

    #endregion

    #region Admin Endpoints

    /// <summary>Admin — get all registered users across all roles</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("all")]
    [ProducesResponseType(typeof(PagedResponse<UserDto>), 200)]
    public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var users = await _userService.GetAllUsersAsync(page, pageSize);
        return Ok(users);
    }

    /// <summary>Admin — change a user's role</summary>
    /// <param name="userId">Target user ID</param>
    [Authorize(Roles = "Admin")]
    [HttpPut("{userId}/role")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ChangeUserRole(string userId, [FromBody] string role)
    {
        await _userService.UpdateUserRoleAsync(userId, role);
        return Ok(new { message = $"User role updated to {role}." });
    }

    /// <summary>Admin — activate or deactivate a user account. Deactivated users cannot log in</summary>
    /// <param name="userId">Target user ID</param>
    [Authorize(Roles = "Admin")]
    [HttpPut("{userId}/status")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ActivateDeactivateUser(string userId, [FromQuery] bool isActive)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _userService.ActivateDeactivateUserAsync(userId, isActive, adminId);
        return Ok(new { message = isActive ? "User account activated." : "User account deactivated." });
    }

    /// <summary>Admin — get all active and revoked user sessions</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(IEnumerable<SessionDto>), 200)]
    public async Task<IActionResult> GetAllSessions()
    {
        var result = await _userService.GetAllSessionsAsync();
        return Ok(result);
    }

    #endregion
}
