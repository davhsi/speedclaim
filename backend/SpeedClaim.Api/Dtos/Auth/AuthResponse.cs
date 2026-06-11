using System;

namespace SpeedClaim.Api.Dtos.Auth;

public class AuthResponse
{
    public string AccessToken { get; }
    public string RefreshToken { get; }
    public AuthUserDto User { get; }

    public AuthResponse(string accessToken, string refreshToken, AuthUserDto user)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        User = user;
    }
}
