using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpeedClaim.Api.Dtos.Auth;
using SpeedClaim.Api.Dtos.Common;
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

    [Authorize(Roles = "Customer")]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _userService.GetProfileAsync(userId);
        return Ok(result);
    }

    [Authorize(Roles = "Customer")]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UserDto request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _userService.UpdateProfileAsync(userId, request);
        return Ok();
    }

    [Authorize(Roles = "Customer")]
    [HttpPost("family")]
    public async Task<IActionResult> AddFamilyMember([FromBody] AddFamilyMemberRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _userService.AddFamilyMemberAsync(userId, request);
        return Ok(result);
    }

    [Authorize(Roles = "Customer")]
    [HttpPut("family/{memberId}")]
    public async Task<IActionResult> UpdateFamilyMember(string memberId, [FromBody] UpdateFamilyMemberRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _userService.UpdateFamilyMemberAsync(memberId, userId, request);
        return Ok();
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("family")]
    public async Task<IActionResult> GetFamilyMembers()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _userService.GetFamilyMembersAsync(userId);
        return Ok(result);
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("kyc")]
    public async Task<IActionResult> GetMyKyc()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var kycRecord = await _userService.GetMyKycAsync(userId);
        if (kycRecord == null) return NotFound("No KYC record found.");
        return Ok(kycRecord);
    }

    [Authorize(Roles = "Customer,Agent")]
    [HttpPost("kyc")]
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

    [Authorize(Roles = "Customer")]
    [HttpPost("addresses")]
    public async Task<IActionResult> AddAddress([FromBody] SingleAddressRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _userService.AddAddressAsync(userId, request);
        return Ok();
    }

    [Authorize(Roles = "Customer")]
    [HttpPut("addresses/{addressId}")]
    public async Task<IActionResult> UpdateAddress(string addressId, [FromBody] SingleAddressRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _userService.UpdateAddressAsync(addressId, userId, request);
        return Ok();
    }

    [Authorize(Roles = "Customer")]
    [HttpDelete("addresses/{addressId}")]
    public async Task<IActionResult> DeleteAddress(string addressId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _userService.DeleteAddressAsync(addressId, userId);
        return Ok();
    }

    [Authorize(Roles = "Customer")]
    [HttpDelete("family/{memberId}")]
    public async Task<IActionResult> DeleteFamilyMember(string memberId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _userService.DeleteFamilyMemberAsync(memberId, userId);
        return Ok();
    }

    [Authorize]
    [HttpGet("notifications")]
    public async Task<IActionResult> GetNotifications()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        var result = await _notificationService.GetForUserAsync(userId);
        return Ok(result);
    }

    [Authorize]
    [HttpPatch("notifications/{id}/read")]
    public async Task<IActionResult> MarkNotificationRead(Guid id)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        await _notificationService.MarkAsReadAsync(id, userId);
        return Ok();
    }

    [Authorize]
    [HttpPatch("notifications/read-all")]
    public async Task<IActionResult> MarkAllNotificationsRead()
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
        await _notificationService.MarkAllAsReadAsync(userId);
        return Ok();
    }

    [Authorize(Roles = "Underwriter,Admin")]
    [HttpGet("kyc/pending")]
    public async Task<IActionResult> GetPendingKyc()
    {
        var result = await _userService.GetPendingKycAsync();
        return Ok(result);
    }

    [Authorize(Roles = "Underwriter,Admin")]
    [HttpPut("{customerId}/kyc/review")]
    public async Task<IActionResult> ApproveRejectKyc(string customerId, [FromQuery] bool isApproved, [FromQuery] string reason)
    {
        var reviewerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _userService.ApproveRejectKycAsync(customerId, isApproved, reason, reviewerId);
        return Ok();
    }
    
    [Authorize(Roles = "Admin")]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{userId}/role")]
    public async Task<IActionResult> ChangeUserRole(string userId, [FromBody] string role)
    {
        await _userService.UpdateUserRoleAsync(userId, role);
        return Ok();
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{userId}/status")]
    public async Task<IActionResult> ActivateDeactivateUser(string userId, [FromQuery] bool isActive)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _userService.ActivateDeactivateUserAsync(userId, isActive, adminId);
        return Ok();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("sessions")]
    public async Task<IActionResult> GetAllSessions()
    {
        var result = await _userService.GetAllSessionsAsync();
        return Ok(result);
    }
}
