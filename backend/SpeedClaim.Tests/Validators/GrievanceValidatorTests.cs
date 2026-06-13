using FluentValidation.TestHelper;
using SpeedClaim.Api.Dtos.Grievances;
using SpeedClaim.Api.Models.Enums;
using SpeedClaim.Api.Validators;

namespace SpeedClaim.Tests.Validators;

[TestFixture]
public class RaiseGrievanceRequestValidatorTests
{
    private RaiseGrievanceRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new RaiseGrievanceRequestValidator();

    [Test]
    public void Valid_Request_Passes()
    {
        var req = new RaiseGrievanceRequest(null, null, GrievanceCategory.ClaimDelay,
            "My claim has been pending for over 30 days without any update from the claims officer.");
        _validator.TestValidate(req).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Invalid_Category_Fails()
    {
        var req = new RaiseGrievanceRequest(null, null, (GrievanceCategory)99,
            "Valid description that is long enough to pass.");
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Category);
    }

    [Test]
    public void Empty_Description_Fails()
    {
        var req = new RaiseGrievanceRequest(null, null, GrievanceCategory.ClaimDelay, "");
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Test]
    public void Too_Short_Description_Fails()
    {
        var req = new RaiseGrievanceRequest(null, null, GrievanceCategory.ClaimDelay, "Too short");
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Test]
    public void Too_Long_Description_Fails()
    {
        var req = new RaiseGrievanceRequest(null, null, GrievanceCategory.ClaimDelay, new string('x', 2001));
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Description);
    }
}

[TestFixture]
public class UpdateGrievanceStatusRequestValidatorTests
{
    private UpdateGrievanceStatusRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateGrievanceStatusRequestValidator();

    [Test]
    public void InProgress_Without_Notes_Passes()
    {
        var req = new UpdateGrievanceStatusRequest(GrievanceStatus.InProgress, null);
        _validator.TestValidate(req).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Resolved_With_Notes_Passes()
    {
        var req = new UpdateGrievanceStatusRequest(GrievanceStatus.Resolved,
            "Issue resolved after coordination with the claims team. Settlement processed.");
        _validator.TestValidate(req).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Resolved_Without_Notes_Fails()
    {
        var req = new UpdateGrievanceStatusRequest(GrievanceStatus.Resolved, null);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.ResolutionNotes);
    }

    [Test]
    public void Closed_Without_Notes_Fails()
    {
        var req = new UpdateGrievanceStatusRequest(GrievanceStatus.Closed, "");
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.ResolutionNotes);
    }

    [Test]
    public void Invalid_Status_Fails()
    {
        var req = new UpdateGrievanceStatusRequest((GrievanceStatus)99, null);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Status);
    }
}
