using System.Threading.Tasks;

namespace SpeedClaim.Api.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody);
}
