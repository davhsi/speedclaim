using System;

namespace SpeedClaim.Api.Dtos.Auth;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    UserDto User
);
