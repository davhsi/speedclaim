using Microsoft.AspNetCore.Authorization;
using SpeedClaim.Api.Controllers;

namespace SpeedClaim.Tests.Controllers;

[TestFixture]
public class ProductBrochuresControllerTests
{
    [Test]
    public void Controller_IsRestrictedToAdminRole()
    {
        var attribute = typeof(ProductBrochuresController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Single();

        Assert.That(attribute.Roles, Is.EqualTo("Admin"));
    }

    [Test]
    public void Controller_DoesNotExposeAnonymousActions()
    {
        var anonymousActions = typeof(ProductBrochuresController)
            .GetMethods()
            .Where(method => method.DeclaringType == typeof(ProductBrochuresController))
            .Where(method => method.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Any())
            .ToList();

        Assert.That(anonymousActions, Is.Empty);
    }
}
