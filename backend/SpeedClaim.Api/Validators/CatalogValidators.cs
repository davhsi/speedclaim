using FluentValidation;
using SpeedClaim.Api.Dtos.Catalog;

namespace SpeedClaim.Api.Validators;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    private static readonly string[] ValidDomains = { "Health", "Life", "Motor" };

    public CreateProductRequestValidator()
    {
        RuleFor(x => x.ProductName)
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters.");

        RuleFor(x => x.Domain)
            .NotEmpty().WithMessage("Domain is required.")
            .Must(d => ValidDomains.Contains(d, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Domain must be one of: Health, Life, Motor.");

        RuleFor(x => x.Uin)
            .NotEmpty().WithMessage("UIN (Unique Identification Number) is required.")
            .MaximumLength(50).WithMessage("UIN cannot exceed 50 characters.");

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

        When(x => x.AllowsFamilyFloater, () =>
        {
            RuleFor(x => x.MaxFamilyMembers)
                .GreaterThan(1).WithMessage("Maximum family members must be at least 2 when family floater is allowed.");
        });
    }
}
