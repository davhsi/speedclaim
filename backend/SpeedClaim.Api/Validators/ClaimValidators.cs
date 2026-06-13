using FluentValidation;
using SpeedClaim.Api.Dtos.Claims;

namespace SpeedClaim.Api.Validators;

public class IntimateClaimRequestValidator : AbstractValidator<IntimateClaimRequest>
{
    public IntimateClaimRequestValidator()
    {
        RuleFor(x => x.PolicyId)
            .NotEmpty().WithMessage("Policy ID is required.");

        RuleFor(x => x.ClaimType)
            .IsInEnum().WithMessage("Invalid claim type.");

        RuleFor(x => x.ClaimAmountRequested)
            .GreaterThan(0).WithMessage("Claim amount must be greater than zero.");

        RuleFor(x => x.IncidentDate)
            .NotEmpty().WithMessage("Incident date is required.")
            .LessThanOrEqualTo(DateTime.UtcNow).WithMessage("Incident date cannot be in the future.");

        RuleFor(x => x.IncidentDescription)
            .NotEmpty().WithMessage("Incident description is required.")
            .MinimumLength(10).WithMessage("Incident description must be at least 10 characters.")
            .MaximumLength(2000).WithMessage("Incident description cannot exceed 2000 characters.");
    }
}

public class ApproveRejectClaimRequestValidator : AbstractValidator<ApproveRejectClaimRequest>
{
    public ApproveRejectClaimRequestValidator()
    {
        When(x => x.IsApproved, () =>
        {
            RuleFor(x => x.ApprovedAmount)
                .NotNull().WithMessage("Approved amount is required when approving a claim.")
                .GreaterThan(0).WithMessage("Approved amount must be greater than zero.");
        });

        When(x => !x.IsApproved, () =>
        {
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Rejection reason is required when rejecting a claim.")
                .MinimumLength(10).WithMessage("Rejection reason must be at least 10 characters.")
                .MaximumLength(1000).WithMessage("Rejection reason cannot exceed 1000 characters.");
        });
    }
}

public class UpdateClaimStatusRequestValidator : AbstractValidator<UpdateClaimStatusRequest>
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Intimated", "DocumentsPending", "PreAuthRequested", "PreAuthApproved",
        "UnderReview", "Approved", "Rejected", "Settled", "Withdrawn"
    };

    public UpdateClaimStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => ValidStatuses.Contains(s))
            .WithMessage("Invalid claim status. Valid values: Intimated, DocumentsPending, PreAuthRequested, PreAuthApproved, UnderReview, Approved, Rejected, Settled, Withdrawn.");

        RuleFor(x => x.Remarks)
            .NotEmpty().WithMessage("Remarks are required when updating claim status.")
            .MaximumLength(1000).WithMessage("Remarks cannot exceed 1000 characters.");
    }
}

public class AssignSurveyorRequestValidator : AbstractValidator<AssignSurveyorRequest>
{
    public AssignSurveyorRequestValidator()
    {
        RuleFor(x => x.SurveyorId)
            .NotEmpty().WithMessage("Surveyor ID is required.");

        RuleFor(x => x.Notes)
            .NotEmpty().WithMessage("Notes are required when assigning a surveyor.")
            .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters.");
    }
}
