using FluentValidation;
using SpeedClaim.Api.Dtos.Auth;

namespace SpeedClaim.Api.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Reset token is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}

public class AdminResetPasswordRequestValidator : AbstractValidator<AdminResetPasswordRequest>
{
    public AdminResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
    }
}

public class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequest>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Verification token is required.");
    }
}

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}

public class ResendVerificationRequestValidator : AbstractValidator<ResendVerificationRequest>
{
    public ResendVerificationRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");
    }
}

public class RegisterAgentRequestValidator : AbstractValidator<RegisterAgentRequest>
{
    public RegisterAgentRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

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

        RuleFor(x => x.LicenseNumber)
            .NotEmpty().WithMessage("License number is required.")
            .MaximumLength(50).WithMessage("License number cannot exceed 50 characters.");

        RuleFor(x => x.LicenseExpiry)
            .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow.Date)).WithMessage("License expiry must be a future date.");

        RuleFor(x => x.AgencyName)
            .NotEmpty().WithMessage("Agency name is required.")
            .MaximumLength(200).WithMessage("Agency name cannot exceed 200 characters.");

        RuleFor(x => x.AadhaarNumber)
            .NotEmpty().WithMessage("Aadhaar number is required.")
            .Matches(@"^\d{12}$").WithMessage("Aadhaar number must be exactly 12 digits.");

        RuleFor(x => x.PanNumber)
            .NotEmpty().WithMessage("PAN number is required.")
            .Matches(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$").WithMessage("PAN number must be in the format ABCDE1234F.");

        RuleFor(x => x.MaritalStatus)
            .IsInEnum().WithMessage("Invalid marital status.");

        RuleFor(x => x.PermanentAddress)
            .NotNull().WithMessage("Permanent address is required.");

        When(x => x.PermanentAddress != null, () =>
        {
            RuleFor(x => x.PermanentAddress.Line1).NotEmpty().WithMessage("Permanent address Line 1 is required.");
            RuleFor(x => x.PermanentAddress.City).NotEmpty().WithMessage("Permanent address city is required.");
            RuleFor(x => x.PermanentAddress.State).NotEmpty().WithMessage("Permanent address state is required.");
            RuleFor(x => x.PermanentAddress.PostalCode)
                .NotEmpty().WithMessage("Permanent address postal code is required.")
                .Matches(@"^\d{6}$").WithMessage("Postal code must be exactly 6 digits.");
            RuleFor(x => x.PermanentAddress.Country).NotEmpty().WithMessage("Permanent address country is required.");
        });

        When(x => !x.IsSameAsPermanent, () =>
        {
            RuleFor(x => x.CurrentAddress).NotNull().WithMessage("Current address is required when not same as permanent.");
            When(x => x.CurrentAddress != null, () =>
            {
                RuleFor(x => x.CurrentAddress!.Line1).NotEmpty().WithMessage("Current address Line 1 is required.");
                RuleFor(x => x.CurrentAddress!.City).NotEmpty().WithMessage("Current address city is required.");
                RuleFor(x => x.CurrentAddress!.State).NotEmpty().WithMessage("Current address state is required.");
                RuleFor(x => x.CurrentAddress!.PostalCode)
                    .NotEmpty().WithMessage("Current address postal code is required.")
                    .Matches(@"^\d{6}$").WithMessage("Postal code must be exactly 6 digits.");
                RuleFor(x => x.CurrentAddress!.Country).NotEmpty().WithMessage("Current address country is required.");
            });
        });
    }
}

public class AgentAddCustomerRequestValidator : AbstractValidator<AgentAddCustomerRequest>
{
    public AgentAddCustomerRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

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

        RuleFor(x => x.DateOfBirth)
            .Must(BeAtLeast18YearsOld).WithMessage("Customer must be at least 18 years old.");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Invalid gender.");

        RuleFor(x => x.MaritalStatus)
            .IsInEnum().WithMessage("Invalid marital status.");

        RuleFor(x => x.PermanentAddress)
            .NotNull().WithMessage("Permanent address is required.");

        When(x => x.PermanentAddress != null, () =>
        {
            RuleFor(x => x.PermanentAddress.Line1).NotEmpty().WithMessage("Permanent address Line 1 is required.");
            RuleFor(x => x.PermanentAddress.City).NotEmpty().WithMessage("Permanent address city is required.");
            RuleFor(x => x.PermanentAddress.State).NotEmpty().WithMessage("Permanent address state is required.");
            RuleFor(x => x.PermanentAddress.PostalCode)
                .NotEmpty().WithMessage("Permanent address postal code is required.")
                .Matches(@"^\d{6}$").WithMessage("Postal code must be exactly 6 digits.");
            RuleFor(x => x.PermanentAddress.Country).NotEmpty().WithMessage("Permanent address country is required.");
        });

        When(x => !x.IsSameAsPermanent, () =>
        {
            RuleFor(x => x.CurrentAddress).NotNull().WithMessage("Current address is required when not same as permanent.");
            When(x => x.CurrentAddress != null, () =>
            {
                RuleFor(x => x.CurrentAddress!.Line1).NotEmpty().WithMessage("Current address Line 1 is required.");
                RuleFor(x => x.CurrentAddress!.City).NotEmpty().WithMessage("Current address city is required.");
                RuleFor(x => x.CurrentAddress!.State).NotEmpty().WithMessage("Current address state is required.");
                RuleFor(x => x.CurrentAddress!.PostalCode)
                    .NotEmpty().WithMessage("Current address postal code is required.")
                    .Matches(@"^\d{6}$").WithMessage("Postal code must be exactly 6 digits.");
                RuleFor(x => x.CurrentAddress!.Country).NotEmpty().WithMessage("Current address country is required.");
            });
        });
    }

    private bool BeAtLeast18YearsOld(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth > today.AddYears(-age)) age--;
        return age >= 18;
    }
}
