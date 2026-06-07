using MailKit.Net.Smtp;

namespace SpeedClaim.Api.Interfaces;

public interface ISmtpClientFactory
{
    ISmtpClient CreateClient();
}
