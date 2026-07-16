using FluentValidation;
using SpeedClaim.Api.Dtos.Catalog;

namespace SpeedClaim.Api.Validators;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    private static readonly string[] ValidDomains = { "Health", "Life", "Motor" };
    private static readonly string[] ValidMotorVehicleTypes = { "TwoWheeler", "PrivateCar", "CommercialVehicle" };

    public CreateProductRequestValidator()
    {
        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters.");

        RuleFor(x => x.Domain)
            .NotEmpty().WithMessage("Domain is required.")
            .Must(d => ValidDomains.Contains(d, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Domain must be one of: Health, Life, Motor.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");

        RuleFor(x => x.MinAge)
            .GreaterThanOrEqualTo(1).WithMessage("Minimum age must be at least 1.");

        RuleFor(x => x.MaxAge)
            .GreaterThan(x => x.MinAge).WithMessage("Maximum age must be greater than minimum age.")
            .LessThanOrEqualTo(100).WithMessage("Maximum age cannot exceed 100.");

        RuleFor(x => x.MinSumAssured)
            .GreaterThan(0).WithMessage("Minimum sum assured must be greater than zero.");

        RuleFor(x => x.MaxSumAssured)
            .GreaterThan(x => x.MinSumAssured).WithMessage("Maximum sum assured must be greater than minimum sum assured.");

        RuleFor(x => x.MinTenureYears)
            .GreaterThanOrEqualTo(1).WithMessage("Minimum tenure must be at least 1 year.");

        RuleFor(x => x.MaxTenureYears)
            .GreaterThanOrEqualTo(x => x.MinTenureYears).WithMessage("Maximum tenure must be greater than or equal to minimum tenure.");

        RuleFor(x => x.WaitingPeriodDays)
            .GreaterThanOrEqualTo(0).WithMessage("Waiting period cannot be negative.");

        RuleFor(x => x.MotorVehicleType)
            .NotEmpty().WithMessage("Motor vehicle type is required for Motor products.")
            .Must(t => t != null && ValidMotorVehicleTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Motor vehicle type must be one of: TwoWheeler, PrivateCar, CommercialVehicle.")
            .When(x => string.Equals(x.Domain, "Motor", StringComparison.OrdinalIgnoreCase));

        RuleFor(x => x.MotorVehicleType)
            .Empty().WithMessage("Motor vehicle type is only available for Motor products.")
            .When(x => !string.Equals(x.Domain, "Motor", StringComparison.OrdinalIgnoreCase));

        RuleFor(x => x.AllowsFamilyFloater)
            .Equal(false)
            .When(x => !string.Equals(x.Domain, "Health", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Family floater is only available for Health products.");

        When(x => x.AllowsFamilyFloater, () =>
        {
            RuleFor(x => x.MaxFamilyMembers)
                .GreaterThan(1).WithMessage("Maximum family members must be at least 2 when family floater is allowed.");
        });
    }
}

public class UploadProductBrochureRequestValidator : AbstractValidator<UploadProductBrochureRequest>
{
    public UploadProductBrochureRequestValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("A brochure PDF is required.")
            .Must(file => file is { Length: > 0 }).WithMessage("The brochure PDF cannot be empty.");

        RuleFor(x => x.EffectiveFrom)
            .NotEmpty().WithMessage("Effective date is required.");

        RuleFor(x => x.Version)
            .Must(version => version == null ||
                (int.TryParse(version, out var value) && value > 0 && value <= 999999))
            .WithMessage("Version must be a positive whole number up to 999999.");
    }
}
