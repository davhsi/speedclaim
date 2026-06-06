using Microsoft.AspNetCore.Mvc;
using SpeedClaim.Api.Dtos.Auth;
using SpeedClaim.Api.Interfaces;
using Asp.Versioning;

namespace SpeedClaim.Api.Controllers;

[ApiVersion("1.0")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register/user")]
    public async Task<IActionResult> RegisterUser([FromBody] RegisterUserRequest request)
    {
        var response = await _authService.RegisterUserAsync(request);
        return Ok(response);
    }

    [HttpPost("register/agent")]
    public async Task<IActionResult> RegisterAgent([FromBody] RegisterAgentRequest request)
    {
        var response = await _authService.RegisterAgentAsync(request);
        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        return Ok(response);
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var response = await _authService.RefreshTokenAsync(request);
        return Ok(response);
    }
}
