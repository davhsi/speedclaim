using FluentValidation.TestHelper;
using SpeedClaim.Api.Dtos.Catalog;
using SpeedClaim.Api.Validators;

namespace SpeedClaim.Tests.Validators;

[TestFixture]
public class CreateProductRequestValidatorTests
{
    private CreateProductRequestValidator _validator = null!;

    private static CreateProductRequest ValidRequest() => new(
        ProductName: "SpeedShield Health Plan",
        Domain: "Health",
        Description: "Comprehensive health insurance covering hospitalisation, day-care, and pre/post hospitalisation expenses.",
        MinAge: 18,
        MaxAge: 65,
        MinSumAssured: 300000m,
        MaxSumAssured: 10000000m,
        MinTenureYears: 1,
        MaxTenureYears: 30,
        WaitingPeriodDays: 30,
        AllowsFamilyFloater: true,
        MaxFamilyMembers: 6
    );

    [SetUp]
    public void SetUp() => _validator = new CreateProductRequestValidator();

    [Test]
    public void Valid_Request_Passes()
    {
        _validator.TestValidate(ValidRequest()).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Empty_ProductName_Fails()
    {
        var req = ValidRequest() with { ProductName = "" };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.ProductName);
    }

    [Test]
    public void Invalid_Domain_Fails()
    {
        var req = ValidRequest() with { Domain = "Dental" };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Domain);
    }

    [Test]
    public void MaxAge_Less_Than_MinAge_Fails()
    {
        var req = ValidRequest() with { MinAge = 50, MaxAge = 30 };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.MaxAge);
    }

    [Test]
    public void MaxSumAssured_Less_Than_MinSumAssured_Fails()
    {
        var req = ValidRequest() with { MinSumAssured = 500000m, MaxSumAssured = 100000m };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.MaxSumAssured);
    }

    [Test]
    public void MaxTenure_Less_Than_MinTenure_Fails()
    {
        var req = ValidRequest() with { MinTenureYears = 10, MaxTenureYears = 5 };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.MaxTenureYears);
    }

    [Test]
    public void FamilyFloater_With_One_Member_Fails()
    {
        var req = ValidRequest() with { AllowsFamilyFloater = true, MaxFamilyMembers = 1 };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.MaxFamilyMembers);
    }

    [TestCase("Motor")]
    [TestCase("Life")]
    public void FamilyFloater_On_NonHealth_Domain_Fails(string domain)
    {
        var req = ValidRequest() with { Domain = domain, AllowsFamilyFloater = true };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.AllowsFamilyFloater);
    }

    [TestCase("Motor")]
    [TestCase("Life")]
    public void NonHealth_Domain_Without_FamilyFloater_Passes(string domain)
    {
        var req = ValidRequest() with { Domain = domain, AllowsFamilyFloater = false, MaxFamilyMembers = 1 };
        _validator.TestValidate(req).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Negative_WaitingPeriod_Fails()
    {
        var req = ValidRequest() with { WaitingPeriodDays = -1 };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.WaitingPeriodDays);
    }
}
