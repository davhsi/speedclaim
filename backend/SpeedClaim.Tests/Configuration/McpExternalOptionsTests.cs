using SpeedClaim.Api.Configuration;

namespace SpeedClaim.Tests.Configuration;

[TestFixture]
public class McpExternalOptionsTests
{
    [Test]
    public void ValidateWhenEnabled_AllowsDisabledConnectorWithoutAuth0Configuration()
    {
        Assert.DoesNotThrow(() => new McpExternalOptions { Enabled = false }.ValidateWhenEnabled());
    }

    [TestCase(null, "speedclaim-mcp", "https://api.example.com")]
    [TestCase("https://tenant.example.com/", null, "https://api.example.com")]
    [TestCase("https://tenant.example.com/", "speedclaim-mcp", null)]
    public void ValidateWhenEnabled_RejectsIncompletePublicConnectorConfiguration(string? issuer, string? audience, string? publicBaseUrl)
    {
        var options = new McpExternalOptions { Enabled = true, Issuer = issuer, Audience = audience, PublicBaseUrl = publicBaseUrl };
        Assert.Throws<InvalidOperationException>(() => options.ValidateWhenEnabled());
    }

    [Test]
    public void ValidateWhenEnabled_AcceptsCompleteConfiguration()
    {
        var options = new McpExternalOptions
        {
            Enabled = true,
            Issuer = "https://speedclaim-dev.us.auth0.com/",
            Audience = "https://mcp.speedclaim.example",
            PublicBaseUrl = "https://api.speedclaim.example"
        };
        Assert.DoesNotThrow(() => options.ValidateWhenEnabled());
    }

    [Test]
    public void ResourceServerIdentifier_UsesCanonicalPublicOriginWithTrailingSlash()
    {
        var options = new McpExternalOptions
        {
            PublicBaseUrl = "https://api.speedclaim.example/"
        };

        Assert.That(options.ResourceServerIdentifier, Is.EqualTo("https://api.speedclaim.example/"));
    }
}
