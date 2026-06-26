using System.Threading.Tasks;

namespace SpeedClaim.Api.Interfaces;

public record EmailAttachment(string FileName, string ContentType, byte[] Content);

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendEmailAsync(string to, string subject, string body, EmailAttachment attachment);
    Task SendEmailVerificationAsync(string to, string token);
    Task SendPasswordResetAsync(string to, string token);
}
