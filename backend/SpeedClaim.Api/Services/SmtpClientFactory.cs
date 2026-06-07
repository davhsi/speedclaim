using MailKit.Net.Smtp;
using SpeedClaim.Api.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace SpeedClaim.Api.Services;

[ExcludeFromCodeCoverage]
public class SmtpClientFactory : ISmtpClientFactory
{
    public ISmtpClient CreateClient()
    {
        return new SmtpClient();
    }
}
