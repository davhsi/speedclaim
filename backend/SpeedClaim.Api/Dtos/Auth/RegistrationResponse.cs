namespace SpeedClaim.Api.Dtos.Auth;

public class RegistrationResponse
{
    public string Email { get; }
    public string Role { get; }

    public RegistrationResponse(string email, string role)
    {
        Email = email;
        Role = role;
    }
}
