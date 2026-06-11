using FluentValidation;
using SpeedClaim.Api.Dtos.Auth;
using System;

namespace SpeedClaim.Api.Validators;

public class RegisterUserRequestValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First Name is required.")
            .MaximumLength(100).WithMessage("First Name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last Name is required.")
            .MaximumLength(100).WithMessage("Last Name cannot exceed 100 characters.");

        RuleFor(x => x.Salutation)
            .NotEmpty().WithMessage("Salutation is required.")
            .Must(BeValidSalutation).WithMessage("Invalid salutation provided.")
            .Must((request, salutation) => MatchGender(request.Gender, salutation))
            .WithMessage("Salutation does not match the selected gender.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of Birth is required.")
            .Must(BeAtLeast18YearsOld).WithMessage("You must be at least 18 years old to register.");

        RuleFor(x => x.AadhaarNumber)
            .NotEmpty().WithMessage("Aadhaar Number is required.")
            .Matches(@"^\d{12}$").WithMessage("Aadhaar Number must be exactly 12 digits.");

        RuleFor(x => x.PanNumber)
            .NotEmpty().WithMessage("PAN Number is required.")
            .Matches(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$").WithMessage("Invalid PAN Number format.");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Invalid Gender selected.");
            
        RuleFor(x => x.MaritalStatus)
            .IsInEnum().WithMessage("Invalid Marital Status selected.");

        RuleFor(x => x).Must(req => 
        {
            var isMrs = req.Salutation?.Equals("Mrs.", StringComparison.OrdinalIgnoreCase) == true;
            if (isMrs && req.MaritalStatus != SpeedClaim.Api.Models.Enums.MaritalStatus.Married)
                return false;
            return true;
        }).WithMessage("If Salutation is Mrs., Marital Status must strictly be Married.");
            
        RuleFor(x => x.PermanentAddress).NotNull().WithMessage("Permanent Address is required.");
        When(x => x.PermanentAddress != null, () =>
        {
            RuleFor(x => x.PermanentAddress.Line1).NotEmpty().WithMessage("Line 1 is required.");
            RuleFor(x => x.PermanentAddress.City).NotEmpty().WithMessage("City is required.");
            RuleFor(x => x.PermanentAddress.State).NotEmpty().WithMessage("State is required.");
            RuleFor(x => x.PermanentAddress.PostalCode).NotEmpty().WithMessage("Postal Code is required.");
            RuleFor(x => x.PermanentAddress.Country).NotEmpty().WithMessage("Country is required.");
        });

        When(x => !x.IsSameAsPermanent, () =>
        {
            RuleFor(x => x.CurrentAddress).NotNull().WithMessage("Current Address is required when not same as permanent.");
            When(x => x.CurrentAddress != null, () =>
            {
                RuleFor(x => x.CurrentAddress!.Line1).NotEmpty().WithMessage("Line 1 is required.");
                RuleFor(x => x.CurrentAddress!.City).NotEmpty().WithMessage("City is required.");
                RuleFor(x => x.CurrentAddress!.State).NotEmpty().WithMessage("State is required.");
                RuleFor(x => x.CurrentAddress!.PostalCode).NotEmpty().WithMessage("Postal Code is required.");
                RuleFor(x => x.CurrentAddress!.Country).NotEmpty().WithMessage("Country is required.");
            });
        });
    }

    private bool BeAtLeast18YearsOld(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        
        return age >= 18;
    }

    private bool BeValidSalutation(string salutation)
    {
        var validSalutations = new[] { "Mr.", "Mrs.", "Ms.", "Mx.", "Dr.", "Prof." };
        return Array.Exists(validSalutations, s => s.Equals(salutation, StringComparison.OrdinalIgnoreCase));
    }

    private bool MatchGender(SpeedClaim.Api.Models.Enums.Gender gender, string salutation)
    {
        var s = salutation?.ToLower();
        if (gender == SpeedClaim.Api.Models.Enums.Gender.Male && (s == "mrs." || s == "ms."))
            return false;
        
        if (gender == SpeedClaim.Api.Models.Enums.Gender.Female && s == "mr.")
            return false;

        return true;
    }
}
