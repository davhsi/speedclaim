namespace SpeedClaim.Api.Dtos.Auth;

public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>Admin force-reset payload — no reset token required (admin is already authorized).</summary>
public class AdminResetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}
