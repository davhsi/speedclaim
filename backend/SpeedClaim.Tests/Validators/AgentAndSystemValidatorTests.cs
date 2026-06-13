using FluentValidation.TestHelper;
using SpeedClaim.Api.Dtos.SystemManagement;
using SpeedClaim.Api.Dtos.User;
using SpeedClaim.Api.Validators;

namespace SpeedClaim.Tests.Validators;

[TestFixture]
public class UpdateAgentProfileRequestValidatorTests
{
    private UpdateAgentProfileRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateAgentProfileRequestValidator();

    [Test]
    public void Valid_Request_Passes()
    {
        var req = new UpdateAgentProfileRequest("Mr", "Raj", "Kumar", "9876543210");
        _validator.TestValidate(req).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Empty_FirstName_Fails()
    {
        var req = new UpdateAgentProfileRequest("Mr", "", "Kumar", "9876543210");
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Test]
    public void Empty_LastName_Fails()
    {
        var req = new UpdateAgentProfileRequest("Mr", "Raj", "", "9876543210");
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Test]
    public void Invalid_Phone_Fails()
    {
        var req = new UpdateAgentProfileRequest("Mr", "Raj", "Kumar", "12345");
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Phone);
    }

    [Test]
    public void Invalid_Salutation_Fails()
    {
        var req = new UpdateAgentProfileRequest("Lord", "Raj", "Kumar", "9876543210");
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Salutation);
    }
}

[TestFixture]
public class UpdateAgentLicenseRequestValidatorTests
{
    private UpdateAgentLicenseRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateAgentLicenseRequestValidator();

    [Test]
    public void Valid_Request_Passes()
    {
        var req = new UpdateAgentLicenseRequest("LIC-2025-001", DateOnly.FromDateTime(DateTime.Today.AddYears(2)));
        _validator.TestValidate(req).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Empty_LicenseNumber_Fails()
    {
        var req = new UpdateAgentLicenseRequest("", DateOnly.FromDateTime(DateTime.Today.AddYears(2)));
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.LicenseNumber);
    }

    [Test]
    public void Past_Expiry_Fails()
    {
        var req = new UpdateAgentLicenseRequest("LIC-2020-001", DateOnly.FromDateTime(DateTime.Today.AddDays(-1)));
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.LicenseExpiry);
    }

    [Test]
    public void Today_Expiry_Fails()
    {
        var req = new UpdateAgentLicenseRequest("LIC-2020-001", DateOnly.FromDateTime(DateTime.Today));
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.LicenseExpiry);
    }
}

[TestFixture]
public class CreateBranchRequestValidatorTests
{
    private CreateBranchRequestValidator _validator = null!;

    private static CreateBranchRequest ValidRequest() => new(
        Name: "Mumbai Central Branch",
        City: "Mumbai",
        State: "Maharashtra",
        Address: "101 Nariman Point, Mumbai - 400021",
        Phone: "9876543210",
        Email: "mumbai@speedclaim.com"
    );

    [SetUp]
    public void SetUp() => _validator = new CreateBranchRequestValidator();

    [Test]
    public void Valid_Request_Passes()
    {
        _validator.TestValidate(ValidRequest()).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Empty_Name_Fails()
    {
        var req = ValidRequest() with { Name = "" };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Test]
    public void Invalid_Phone_Fails()
    {
        var req = ValidRequest() with { Phone = "98765" };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Phone);
    }

    [Test]
    public void Invalid_Email_Fails()
    {
        var req = ValidRequest() with { Email = "not-an-email" };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Test]
    public void Empty_City_Fails()
    {
        var req = ValidRequest() with { City = "" };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.City);
    }
}

[TestFixture]
public class UpdateSystemConfigRequestValidatorTests
{
    private UpdateSystemConfigRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateSystemConfigRequestValidator();

    [Test]
    public void Valid_Request_Passes()
    {
        var req = new UpdateSystemConfigRequest("max_claim_amount", "5000000");
        _validator.TestValidate(req).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Empty_Key_Fails()
    {
        var req = new UpdateSystemConfigRequest("", "5000000");
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.ConfigKey);
    }

    [Test]
    public void Empty_Value_Fails()
    {
        var req = new UpdateSystemConfigRequest("max_claim_amount", "");
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.ConfigValue);
    }
}

[TestFixture]
public class ManageEmailTemplateRequestValidatorTests
{
    private ManageEmailTemplateRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new ManageEmailTemplateRequestValidator();

    [Test]
    public void Valid_Request_Passes()
    {
        var req = new ManageEmailTemplateRequest(
            "CLAIM_APPROVED",
            "Your Claim Has Been Approved",
            "<p>Dear {{Name}}, your claim {{ClaimNumber}} has been approved.</p>"
        );
        _validator.TestValidate(req).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Empty_TemplateKey_Fails()
    {
        var req = new ManageEmailTemplateRequest("", "Subject", "<p>Body</p>");
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.TemplateKey);
    }

    [Test]
    public void Empty_Subject_Fails()
    {
        var req = new ManageEmailTemplateRequest("KEY", "", "<p>Body</p>");
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Subject);
    }

    [Test]
    public void Empty_Body_Fails()
    {
        var req = new ManageEmailTemplateRequest("KEY", "Subject", "");
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.BodyHtml);
    }
}
