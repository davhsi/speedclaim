using System.Security.Claims;
using SpeedClaim.Api.Mcp;

namespace SpeedClaim.Tests.Mcp;

[TestFixture]
public class McpEndpointRouteBuilderExtensionsTests
{
    [Test]
    public void HasScope_RecognizesStandardSpaceDelimitedOAuthScopeClaim()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("scope", "speedclaim.catalog.read speedclaim.account.read")
        ]));

        Assert.That(McpEndpointRouteBuilderExtensions.HasScope(principal, "speedclaim.catalog.read"), Is.True);
        Assert.That(McpEndpointRouteBuilderExtensions.HasScope(principal, "speedclaim.account.read"), Is.True);
    }

    [Test]
    public void HasScope_RecognizesAuth0JsonArrayPermissionsClaim()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("permissions", "[\"speedclaim.catalog.read\",\"speedclaim.account.read\"]")
        ]));

        Assert.That(McpEndpointRouteBuilderExtensions.HasScope(principal, "speedclaim.catalog.read"), Is.True);
        Assert.That(McpEndpointRouteBuilderExtensions.HasScope(principal, "speedclaim.account.read"), Is.True);
    }

    [Test]
    public void HasScope_RecognizesNamespacedPermissionsClaim()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("https://speedclaim.example/permissions", "[\"speedclaim.catalog.read\"]")
        ]));

        Assert.That(McpEndpointRouteBuilderExtensions.HasScope(principal, "speedclaim.catalog.read"), Is.True);
    }

    [Test]
    public void HasScope_DoesNotTreatUnrelatedPermissionAsAuthorized()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("permissions", "[\"speedclaim.catalog.read\"]")
        ]));

        Assert.That(McpEndpointRouteBuilderExtensions.HasScope(principal, "speedclaim.account.read"), Is.False);
    }
}
