using FluentValidation.TestHelper;
using SpeedClaim.Api.Dtos.Claims;
using SpeedClaim.Api.Models.Enums;
using SpeedClaim.Api.Validators;

namespace SpeedClaim.Tests.Validators;

[TestFixture]
public class IntimateClaimRequestValidatorTests
{
    private IntimateClaimRequestValidator _validator = null!;

    private static IntimateClaimRequest ValidRequest(decimal claimAmountRequested = 500m) => new(
        PolicyId: Guid.NewGuid(),
        ClaimantMemberId: null,
        ClaimType: ClaimType.Health,
        ClaimAmountRequested: claimAmountRequested,
        IncidentDate: DateTime.UtcNow.Date,
        IncidentDescription: "Hospitalisation expenses for a covered incident.",
        IsCashless: false
    );

    [SetUp]
    public void SetUp() => _validator = new IntimateClaimRequestValidator();

    [Test]
    public void Minimum_ClaimAmount_Passes()
    {
        var req = ValidRequest(IntimateClaimRequestValidator.MinimumClaimAmount);

        _validator.TestValidate(req).ShouldNotHaveValidationErrorFor(x => x.ClaimAmountRequested);
    }

    [Test]
    public void Below_Minimum_ClaimAmount_Fails()
    {
        var req = ValidRequest(IntimateClaimRequestValidator.MinimumClaimAmount - 0.01m);

        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.ClaimAmountRequested);
    }
}
