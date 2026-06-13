using FluentValidation;
using SpeedClaim.Api.Dtos.Policies;
using SpeedClaim.Api.Dtos.Sales;

namespace SpeedClaim.Api.Validators;

public class GenerateQuoteRequestValidator : AbstractValidator<GenerateQuoteRequest>
{
    public GenerateQuoteRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.");

        RuleFor(x => x.Age)
            .InclusiveBetween(1, 100).WithMessage("Age must be between 1 and 100.");

        RuleFor(x => x.SumAssured)
            .GreaterThan(0).WithMessage("Sum assured must be greater than zero.");

        RuleFor(x => x.TenureYears)
            .GreaterThan(0).WithMessage("Tenure must be at least 1 year.");
    }
}

public class SubmitProposalRequestValidator : AbstractValidator<SubmitProposalRequest>
{
    public SubmitProposalRequestValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required.")
            .Must(id => Guid.TryParse(id, out _)).WithMessage("Customer ID must be a valid GUID.");

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID is required.")
            .Must(id => Guid.TryParse(id, out _)).WithMessage("Product ID must be a valid GUID.");

        RuleFor(x => x.SumAssured)
            .GreaterThan(0).WithMessage("Sum assured must be greater than zero.");

        RuleFor(x => x.TenureYears)
            .GreaterThan(0).WithMessage("Tenure must be at least 1 year.");

        RuleFor(x => x.PremiumAmount)
            .GreaterThan(0).WithMessage("Premium amount must be greater than zero.");

        RuleFor(x => x.PaymentFrequency)
            .NotEmpty().WithMessage("Payment frequency is required.")
            .Must(f => new[] { "Monthly", "Quarterly", "HalfYearly", "Annually" }.Contains(f, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Payment frequency must be one of: Monthly, Quarterly, HalfYearly, Annually.");

        When(x => x.Nominees != null && x.Nominees.Count > 0, () =>
        {
            RuleForEach(x => x.Nominees).SetValidator(new NomineeDtoValidator());

            RuleFor(x => x.Nominees)
                .Must(nominees => nominees == null || Math.Abs(nominees.Sum(n => n.SharePercentage) - 100) < 0.01m)
                .WithMessage("Nominee share percentages must add up to exactly 100%.");
        });

        When(x => x.MotorDetail != null, () =>
        {
            RuleFor(x => x.MotorDetail!).SetValidator(new MotorDetailDtoValidator());
        });
    }
}

public class NomineeDtoValidator : AbstractValidator<NomineeDto>
{
    public NomineeDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Nominee full name is required.")
            .MaximumLength(100).WithMessage("Nominee full name cannot exceed 100 characters.");

        RuleFor(x => x.Relationship)
            .NotEmpty().WithMessage("Nominee relationship is required.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Nominee date of birth is required.")
            .LessThan(DateOnly.FromDateTime(DateTime.Today)).WithMessage("Nominee date of birth must be in the past.");

        RuleFor(x => x.SharePercentage)
            .InclusiveBetween(0.01m, 100m).WithMessage("Nominee share percentage must be between 0.01 and 100.");

        When(x => x.IsMinor, () =>
        {
            RuleFor(x => x.AppointeeName)
                .NotEmpty().WithMessage("Appointee name is required for a minor nominee.");
        });
    }
}

public class MotorDetailDtoValidator : AbstractValidator<MotorDetailDto>
{
    public MotorDetailDtoValidator()
    {
        RuleFor(x => x.VehicleNumber)
            .NotEmpty().WithMessage("Vehicle number is required.")
            .MaximumLength(20).WithMessage("Vehicle number cannot exceed 20 characters.");

        RuleFor(x => x.VehicleMake)
            .NotEmpty().WithMessage("Vehicle make is required.")
            .MaximumLength(100).WithMessage("Vehicle make cannot exceed 100 characters.");

        RuleFor(x => x.VehicleModel)
            .NotEmpty().WithMessage("Vehicle model is required.")
            .MaximumLength(100).WithMessage("Vehicle model cannot exceed 100 characters.");

        RuleFor(x => x.ManufactureYear)
            .InclusiveBetween(1900, DateTime.Today.Year)
            .WithMessage($"Manufacture year must be between 1900 and {DateTime.Today.Year}.");

        RuleFor(x => x.EngineNumber)
            .NotEmpty().WithMessage("Engine number is required.")
            .MaximumLength(50).WithMessage("Engine number cannot exceed 50 characters.");

        RuleFor(x => x.ChassisNumber)
            .NotEmpty().WithMessage("Chassis number is required.")
            .MaximumLength(50).WithMessage("Chassis number cannot exceed 50 characters.");

        RuleFor(x => x.Idv)
            .GreaterThan(0).WithMessage("IDV (Insured Declared Value) must be greater than zero.");
    }
}

public class RequestEndorsementRequestValidator : AbstractValidator<RequestEndorsementRequest>
{
    public RequestEndorsementRequestValidator()
    {
        RuleFor(x => x.EndorsementType)
            .IsInEnum().WithMessage("Invalid endorsement type.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MinimumLength(10).WithMessage("Description must be at least 10 characters.")
            .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");
    }
}

public class ApproveRejectEndorsementRequestValidator : AbstractValidator<ApproveRejectEndorsementRequest>
{
    public ApproveRejectEndorsementRequestValidator()
    {
        When(x => !x.IsApproved, () =>
        {
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Reason is required when rejecting an endorsement.")
                .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters.");
        });
    }
}

public class UpdateNomineeRequestValidator : AbstractValidator<UpdateNomineeRequest>
{
    public UpdateNomineeRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.");

        RuleFor(x => x.Relationship)
            .NotEmpty().WithMessage("Relationship is required.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .LessThan(DateOnly.FromDateTime(DateTime.Today)).WithMessage("Date of birth must be in the past.");

        RuleFor(x => x.SharePercentage)
            .InclusiveBetween(0.01m, 100m).WithMessage("Share percentage must be between 0.01 and 100.");

        When(x => x.IsMinor, () =>
        {
            RuleFor(x => x.AppointeeName)
                .NotEmpty().WithMessage("Appointee name is required for a minor nominee.");
        });
    }
}
