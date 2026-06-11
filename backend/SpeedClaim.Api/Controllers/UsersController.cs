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
    private readonly INotificationService _notificationService;

    public UsersController(IUserService userService, INotificationService notificationService)
    {
        _userService = userService;
        _notificationService = notificationService;
    }

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

    /// <summary>Update the authenticated customer's profile (name, phone, marital status, salutation)</summary>
    [Authorize(Roles = "Customer")]
    [HttpPut("profile")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateProfile([FromBody] UserDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _userService.UpdateProfileAsync(userId, request);
        return Ok();
    }

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
        return Ok();
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

    /// <summary>Get the KYC record for the authenticated customer</summary>
    [Authorize(Roles = "Customer")]
    [HttpGet("kyc")]
    [ProducesResponseType(typeof(KycRecordDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetMyKyc()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var kycRecord = await _userService.GetMyKycAsync(userId);
        if (kycRecord == null) return NotFound("No KYC record found.");
        return Ok(kycRecord);
    }

    /// <summary>Upload KYC identity documents (front and optional back image)</summary>
    /// <remarks>Agents can upload on behalf of a customer by providing CustomerId in the form. Resets status to Pending on re-upload.</remarks>
    [Authorize(Roles = "Customer,Agent")]
    [HttpPost("kyc")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> UploadKycDocuments([FromForm] KycUploadRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var role = User.FindFirstValue(ClaimTypes.Role);

        if (role == "Agent" && request.CustomerId.HasValue)
        {
            userId = request.CustomerId.Value.ToString();
        }

        await _userService.UploadKycDocumentsAsync(userId, request);
        return Ok();
    }

    /// <summary>Add a new address (Permanent or Current) to the customer's profile</summary>
    [Authorize(Roles = "Customer")]
    [HttpPost("addresses")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> AddAddress([FromBody] SingleAddressRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _userService.AddAddressAsync(userId, request);
        return Ok();
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
        return Ok();
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
        return Ok();
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
        return Ok();
    }

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
        return Ok();
    }

    /// <summary>Mark all unread notifications as read for the authenticated user</summary>
    [Authorize]
    [HttpPatch("notifications/read-all")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> MarkAllNotificationsRead()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        await _notificationService.MarkAllAsReadAsync(userId);
        return Ok();
    }

    /// <summary>Get all customer KYC submissions currently in Pending status awaiting review</summary>
    [Authorize(Roles = "Underwriter,Admin")]
    [HttpGet("kyc/pending")]
    [ProducesResponseType(typeof(IEnumerable<KycRecordDto>), 200)]
    public async Task<IActionResult> GetPendingKyc()
    {
        var result = await _userService.GetPendingKycAsync();
        return Ok(result);
    }

    /// <summary>Approve or reject a customer's KYC submission</summary>
    /// <param name="customerId">Customer user ID</param>
    [Authorize(Roles = "Underwriter,Admin")]
    [HttpPut("{customerId}/kyc/review")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ApproveRejectKyc(string customerId, [FromQuery] bool isApproved, [FromQuery] string reason)
    {
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _userService.ApproveRejectKycAsync(customerId, isApproved, reason, reviewerId);
        return Ok();
    }

    /// <summary>Admin — get all registered users across all roles</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("all")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), 200)]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
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
        return Ok();
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
        return Ok();
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
}
