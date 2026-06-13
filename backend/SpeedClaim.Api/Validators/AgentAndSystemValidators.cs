using FluentValidation;
using SpeedClaim.Api.Dtos.SystemManagement;
using SpeedClaim.Api.Dtos.User;

namespace SpeedClaim.Api.Validators;

public class UpdateAgentProfileRequestValidator : AbstractValidator<UpdateAgentProfileRequest>
{
    public UpdateAgentProfileRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Salutation)
            .NotEmpty().WithMessage("Salutation is required.")
            .Must(s => new[] { "Mr", "Mrs", "Ms", "Dr", "Prof" }.Contains(s, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Salutation must be one of: Mr, Mrs, Ms, Dr, Prof.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^\d{10}$").WithMessage("Phone number must be exactly 10 digits.");
    }
}

public class UpdateAgentLicenseRequestValidator : AbstractValidator<UpdateAgentLicenseRequest>
{
    public UpdateAgentLicenseRequestValidator()
    {
        RuleFor(x => x.LicenseNumber)
            .NotEmpty().WithMessage("License number is required.")
            .MaximumLength(50).WithMessage("License number cannot exceed 50 characters.");

        RuleFor(x => x.LicenseExpiry)
            .NotEmpty().WithMessage("License expiry date is required.")
            .GreaterThan(DateOnly.FromDateTime(DateTime.Today)).WithMessage("License expiry date must be in the future.");
    }
}

public class CreateBranchRequestValidator : AbstractValidator<CreateBranchRequest>
{
    public CreateBranchRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Branch name is required.")
            .MaximumLength(200).WithMessage("Branch name cannot exceed 200 characters.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City cannot exceed 100 characters.");

        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State is required.")
            .MaximumLength(100).WithMessage("State cannot exceed 100 characters.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(300).WithMessage("Address cannot exceed 300 characters.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^\d{10}$").WithMessage("Phone number must be exactly 10 digits.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");
    }
}

public class UpdateSystemConfigRequestValidator : AbstractValidator<UpdateSystemConfigRequest>
{
    public UpdateSystemConfigRequestValidator()
    {
        RuleFor(x => x.ConfigKey)
            .NotEmpty().WithMessage("Config key is required.")
            .MaximumLength(100).WithMessage("Config key cannot exceed 100 characters.");

        RuleFor(x => x.ConfigValue)
            .NotEmpty().WithMessage("Config value is required.")
            .MaximumLength(2000).WithMessage("Config value cannot exceed 2000 characters.");
    }
}

public class ManageEmailTemplateRequestValidator : AbstractValidator<ManageEmailTemplateRequest>
{
    public ManageEmailTemplateRequestValidator()
    {
        RuleFor(x => x.TemplateKey)
            .NotEmpty().WithMessage("Template key is required.")
            .MaximumLength(100).WithMessage("Template key cannot exceed 100 characters.");

        RuleFor(x => x.Subject)
            .NotEmpty().WithMessage("Email subject is required.")
            .MaximumLength(200).WithMessage("Email subject cannot exceed 200 characters.");

        RuleFor(x => x.BodyHtml)
            .NotEmpty().WithMessage("Email body is required.");
    }
}
