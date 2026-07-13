using FluentValidation.TestHelper;
using SpeedClaim.Api.Dtos.Sales;
using SpeedClaim.Api.Dtos.Policies;
using SpeedClaim.Api.Validators;

namespace SpeedClaim.Tests.Validators;

[TestFixture]
public class GenerateQuoteRequestValidatorTests
{
    private GenerateQuoteRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new GenerateQuoteRequestValidator();

    [Test]
    public void Valid_Request_Passes()
    {
        var req = new GenerateQuoteRequest("prod-001", 35, 500000m, 10);
        _validator.TestValidate(req).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Empty_ProductId_Fails()
    {
        var req = new GenerateQuoteRequest("", 35, 500000m, 10);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Test]
    public void Age_Zero_Fails()
    {
        var req = new GenerateQuoteRequest("prod-001", 0, 500000m, 10);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Age);
    }

    [Test]
    public void Age_Over_100_Fails()
    {
        var req = new GenerateQuoteRequest("prod-001", 101, 500000m, 10);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Age);
    }

    [Test]
    public void Null_Age_Passes_ProductLevelEnforcementHandlesRequiredness()
    {
        // Motor quotes never send an age — the DTO-level validator allows null; whether an
        // age is actually required is a product/domain concern enforced in ProposalService.
        var req = new GenerateQuoteRequest("prod-001", null, 500000m, 10);
        _validator.TestValidate(req).ShouldNotHaveValidationErrorFor(x => x.Age);
    }

    [Test]
    public void Zero_SumAssured_Fails()
    {
        var req = new GenerateQuoteRequest("prod-001", 35, 0m, 10);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.SumAssured);
    }

    [Test]
    public void Zero_Tenure_Fails()
    {
        var req = new GenerateQuoteRequest("prod-001", 35, 500000m, 0);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.TenureYears);
    }
}

[TestFixture]
public class SubmitProposalRequestValidatorTests
{
    private SubmitProposalRequestValidator _validator = null!;

    private static SubmitProposalRequest ValidRequest(List<NomineeDto>? nominees = null) => new(
        CustomerId: Guid.NewGuid().ToString(),
        ProductId: Guid.NewGuid().ToString(),
        SumAssured: 1000000m,
        TenureYears: 10,
        PremiumAmount: 12000m,
        PaymentFrequency: "Annually",
        HealthDetail: null,
        LifeDetail: null,
        MotorDetail: null,
        CustomerMemberIds: new List<string>(),
        Nominees: nominees ?? new List<NomineeDto>()
    );

    [SetUp]
    public void SetUp() => _validator = new SubmitProposalRequestValidator();

    [Test]
    public void Valid_Request_No_Nominees_Passes()
    {
        _validator.TestValidate(ValidRequest()).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Valid_Request_With_Nominees_Summing_100_Passes()
    {
        var nominees = new List<NomineeDto>
        {
            new("Priya Kumar", "Spouse", new DateOnly(1990, 5, 1), 60m, false, null),
            new("Arjun Kumar", "Son",   new DateOnly(2015, 3, 10), 40m, true,  "Priya Kumar")
        };
        _validator.TestValidate(ValidRequest(nominees)).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Invalid_CustomerId_Guid_Fails()
    {
        var req = ValidRequest() with { CustomerId = "not-a-guid" };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    [Test]
    public void Zero_SumAssured_Fails()
    {
        var req = ValidRequest() with { SumAssured = 0m };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.SumAssured);
    }

    [Test]
    public void Invalid_PaymentFrequency_Fails()
    {
        var req = ValidRequest() with { PaymentFrequency = "Weekly" };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.PaymentFrequency);
    }

    [Test]
    public void Nominees_Not_Summing_To_100_Fails()
    {
        var nominees = new List<NomineeDto>
        {
            new("Priya Kumar", "Spouse", new DateOnly(1990, 5, 1), 60m, false, null),
            new("Arjun Kumar", "Son",   new DateOnly(2015, 3, 10), 30m, true,  "Priya Kumar")
        };
        var result = _validator.TestValidate(ValidRequest(nominees));
        result.ShouldHaveValidationErrorFor(x => x.Nominees);
    }

    [Test]
    public void Minor_Nominee_Without_Appointee_Fails()
    {
        var nominees = new List<NomineeDto>
        {
            new("Arjun Kumar", "Son", new DateOnly(2015, 3, 10), 100m, true, null)
        };
        var result = _validator.TestValidate(ValidRequest(nominees));
        result.ShouldHaveAnyValidationError();
    }

    [Test]
    public void Nominee_Future_DOB_Fails()
    {
        var nominees = new List<NomineeDto>
        {
            new("Future Person", "Son", DateOnly.FromDateTime(DateTime.Today.AddYears(1)), 100m, false, null)
        };
        var result = _validator.TestValidate(ValidRequest(nominees));
        result.ShouldHaveAnyValidationError();
    }
}

[TestFixture]
public class RequestEndorsementRequestValidatorTests
{
    private RequestEndorsementRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new RequestEndorsementRequestValidator();

    [Test]
    public void Valid_Request_Passes()
    {
        var req = new RequestEndorsementRequest(
            SpeedClaim.Api.Models.Enums.EndorsementType.AddressChange,
            "Requesting address update from old city to new city after relocation.",
            "123 Old Street, Mumbai",
            "456 New Street, Pune"
        );
        _validator.TestValidate(req).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Empty_Description_Fails()
    {
        var req = new RequestEndorsementRequest(
            SpeedClaim.Api.Models.Enums.EndorsementType.AddressChange, "", null, null);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Test]
    public void Too_Short_Description_Fails()
    {
        var req = new RequestEndorsementRequest(
            SpeedClaim.Api.Models.Enums.EndorsementType.AddressChange, "Short", null, null);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Description);
    }
}

[TestFixture]
public class ApproveRejectEndorsementRequestValidatorTests
{
    private ApproveRejectEndorsementRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new ApproveRejectEndorsementRequestValidator();

    [Test]
    public void Approval_Passes()
    {
        var req = new ApproveRejectEndorsementRequest(IsApproved: true, Reason: "");
        _validator.TestValidate(req).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Rejection_With_Reason_Passes()
    {
        var req = new ApproveRejectEndorsementRequest(IsApproved: false, Reason: "Supporting documents are insufficient.");
        _validator.TestValidate(req).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Rejection_Without_Reason_Fails()
    {
        var req = new ApproveRejectEndorsementRequest(IsApproved: false, Reason: "");
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Reason);
    }
}

[TestFixture]
public class UpdateNomineeRequestValidatorTests
{
    private UpdateNomineeRequestValidator _validator = null!;

    [SetUp]
    public void SetUp() => _validator = new UpdateNomineeRequestValidator();

    [Test]
    public void Valid_Request_Passes()
    {
        var req = new UpdateNomineeRequest("Priya Kumar", "Spouse", new DateOnly(1990, 5, 1), 100m, false, null);
        _validator.TestValidate(req).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Empty_FullName_Fails()
    {
        var req = new UpdateNomineeRequest("", "Spouse", new DateOnly(1990, 5, 1), 100m, false, null);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Test]
    public void Share_Over_100_Fails()
    {
        var req = new UpdateNomineeRequest("Priya", "Spouse", new DateOnly(1990, 5, 1), 101m, false, null);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.SharePercentage);
    }

    [Test]
    public void Zero_Share_Fails()
    {
        var req = new UpdateNomineeRequest("Priya", "Spouse", new DateOnly(1990, 5, 1), 0m, false, null);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.SharePercentage);
    }

    [Test]
    public void Minor_Without_Appointee_Fails()
    {
        var req = new UpdateNomineeRequest("Arjun", "Son", new DateOnly(2015, 1, 1), 100m, true, null);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.AppointeeName);
    }

    [Test]
    public void Future_DOB_Fails()
    {
        var req = new UpdateNomineeRequest("Priya", "Spouse", DateOnly.FromDateTime(DateTime.Today.AddYears(1)), 100m, false, null);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.DateOfBirth);
    }
}
