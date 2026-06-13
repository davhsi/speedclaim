using FluentValidation;
using SpeedClaim.Api.Dtos.Grievances;

namespace SpeedClaim.Api.Validators;

public class RaiseGrievanceRequestValidator : AbstractValidator<RaiseGrievanceRequest>
{
    public RaiseGrievanceRequestValidator()
    {
        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Invalid grievance category.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Grievance description is required.")
            .MinimumLength(10).WithMessage("Description must be at least 10 characters.")
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");
    }
}

public class UpdateGrievanceStatusRequestValidator : AbstractValidator<UpdateGrievanceStatusRequest>
{
    public UpdateGrievanceStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Invalid grievance status.");

        When(x => x.Status == SpeedClaim.Api.Models.Enums.GrievanceStatus.Resolved ||
                  x.Status == SpeedClaim.Api.Models.Enums.GrievanceStatus.Closed, () =>
        {
            RuleFor(x => x.ResolutionNotes)
                .NotEmpty().WithMessage("Resolution notes are required when resolving or closing a grievance.")
                .MaximumLength(2000).WithMessage("Resolution notes cannot exceed 2000 characters.");
        });
    }
}
