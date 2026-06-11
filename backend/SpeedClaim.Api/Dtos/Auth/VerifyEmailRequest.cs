using System;

namespace SpeedClaim.Api.Dtos.Auth;

public record VerifyEmailRequest(
    string Token
);
