using System.Threading.Tasks;

namespace SpeedClaim.Api.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendEmailVerificationAsync(string to, string token);
    Task SendPasswordResetAsync(string to, string token);
}
