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
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full Name is required.");

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
            
        RuleFor(x => x.Address).NotNull().WithMessage("Address is required.");
        When(x => x.Address != null, () =>
        {
            RuleFor(x => x.Address.Street).NotEmpty().WithMessage("Street is required.");
            RuleFor(x => x.Address.City).NotEmpty().WithMessage("City is required.");
            RuleFor(x => x.Address.State).NotEmpty().WithMessage("State is required.");
            RuleFor(x => x.Address.PostalCode).NotEmpty().WithMessage("Postal Code is required.");
            RuleFor(x => x.Address.Country).NotEmpty().WithMessage("Country is required.");
        });
    }

    private bool BeAtLeast18YearsOld(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        
        return age >= 18;
    }
}
