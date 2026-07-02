using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SpeedClaim.Api.Dtos.Auth;
using SpeedClaim.Api.Interfaces;
using System.Security.Claims;

namespace SpeedClaim.Api.Controllers;

public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    #region Public Endpoints

    /// <summary>Register a new customer account</summary>
    /// <remarks>Sends a verification email after successful registration. Account is inactive until email is verified.</remarks>
    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(RegistrationResponse), 200)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Register([FromBody] RegisterUserRequest request)
    {
        var result = await _authService.RegisterCustomerAsync(request);
        return Ok(result);
    }

    /// <summary>Authenticate a user and obtain JWT access and refresh tokens</summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(result);
    }

    /// <summary>Verify a customer's email address using the token sent during registration</summary>
    [HttpPost("verify-email")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        await _authService.VerifyEmailAsync(request.Token);
        return Ok(new { message = "Email verified successfully. You can now log in." });
    }

    /// <summary>Resend the email verification link</summary>
    /// <remarks>Always returns 200 regardless of whether the email exists or is already verified, to prevent email enumeration.</remarks>
    [HttpPost("resend-verification")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ResendVerificationEmail([FromBody] ResendVerificationRequest request)
    {
        await _authService.ResendVerificationEmailAsync(request.Email);
        return Ok(new { message = "If that email is registered and unverified, a new verification link has been sent." });
    }

    /// <summary>Exchange a valid refresh token for a new access token and refresh token pair</summary>
    /// <remarks>The old refresh token is revoked and replaced. Tokens use the format `sessionId:rawToken`.</remarks>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        return Ok(result);
    }

    /// <summary>Revoke all active sessions for the authenticated user</summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        if (userId != null)
        {
            await _authService.LogoutAsync(userId);
        }
        return Ok(new { message = "Logged out successfully. All sessions have been revoked." });
    }

    /// <summary>Request a password reset email</summary>
    /// <remarks>Always returns 200 regardless of whether the email exists, to prevent email enumeration.</remarks>
    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAsync(request.Email);
        return Ok(new { message = "If that email is registered, a password reset link has been sent." });
    }

    /// <summary>Reset a customer's password using the token from the reset email</summary>
    [HttpPost("reset-password")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ResetPasswordCustomer([FromBody] ResetPasswordRequest request)
    {
        await _authService.ResetPasswordCustomerAsync(request);
        return Ok(new { message = "Password reset successfully. Please log in with your new password." });
    }

    #endregion

    #region Admin Endpoints

    /// <summary>Admin — force-reset any user's password by user ID</summary>
    /// <param name="userId">Target user ID</param>
    [Authorize(Roles = "Admin")]
    [HttpPost("admin/reset-password/{userId}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ResetPassword(string userId, [FromBody] AdminResetPasswordRequest request)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await _authService.ResetPasswordAsync(userId, request.NewPassword, adminId ?? string.Empty);
        return Ok(new { message = "Password reset successfully" });
    }

    /// <summary>Admin — create a new agent account</summary>
    /// <remarks>Agent accounts are pre-verified and active on creation; no email verification step.</remarks>
    [Authorize(Roles = "Admin")]
    [HttpPost("admin/register-agent")]
    [ProducesResponseType(typeof(RegistrationResponse), 200)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> RegisterAgent([FromBody] RegisterAgentRequest request)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _authService.RegisterAgentAsync(request, adminId);
        return Ok(result);
    }

    /// <summary>Admin — invite a staff user (Underwriter, ClaimsOfficer, FinanceOfficer, Surveyor, Admin)</summary>
    /// <remarks>Creates the account as active and sends a password-reset link so the user sets their own password.</remarks>
    [Authorize(Roles = "Admin")]
    [HttpPost("admin/invite-user")]
    [ProducesResponseType(typeof(RegistrationResponse), 200)]
    [ProducesResponseType(409)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> InviteUser([FromBody] AdminInviteUserRequest request)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var result = await _authService.InviteStaffUserAsync(request, adminId);
        return Ok(result);
    }

    #endregion
}
