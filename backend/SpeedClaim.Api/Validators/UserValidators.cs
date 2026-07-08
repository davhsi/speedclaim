using FluentValidation;
using SpeedClaim.Api.Dtos.User;

namespace SpeedClaim.Api.Validators;

public class AddFamilyMemberRequestValidator : AbstractValidator<AddFamilyMemberRequest>
{
    public AddFamilyMemberRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Salutation)
            .IsInEnum().WithMessage("Invalid salutation.");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Invalid gender.");

        RuleFor(x => x.Relationship)
            .IsInEnum().WithMessage("Invalid relationship.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .LessThan(DateOnly.FromDateTime(DateTime.Today)).WithMessage("Date of birth must be in the past.");
    }
}

public class UpdateFamilyMemberRequestValidator : AbstractValidator<UpdateFamilyMemberRequest>
{
    public UpdateFamilyMemberRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(100).WithMessage("First name cannot exceed 100 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(100).WithMessage("Last name cannot exceed 100 characters.");

        RuleFor(x => x.Salutation)
            .IsInEnum().WithMessage("Invalid salutation.");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("Invalid gender.");

        RuleFor(x => x.Relationship)
            .IsInEnum().WithMessage("Invalid relationship.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .LessThan(DateOnly.FromDateTime(DateTime.Today)).WithMessage("Date of birth must be in the past.");
    }
}

public class AadhaarUploadRequestValidator : AbstractValidator<AadhaarUploadRequest>
{
    private static readonly long MaxFileSizeBytes = 5 * 1024 * 1024;

    public AadhaarUploadRequestValidator()
    {
        RuleFor(x => x.AadhaarNumber)
            .NotEmpty().WithMessage("Aadhaar number is required.")
            .Matches(@"^\d{12}$").WithMessage("Aadhaar number must be exactly 12 digits.");

        RuleFor(x => x.Document)
            .NotNull().WithMessage("Document is required.")
            .Must(f => f != null && f.Length > 0).WithMessage("Document cannot be empty.")
            .Must(f => f == null || f.Length <= MaxFileSizeBytes).WithMessage("Document must not exceed 5 MB.")
            .Must(f => f == null || IsAllowedFileType(f.FileName))
            .WithMessage("Document must be a PDF, JPG, JPEG, or PNG file.");
    }

    private static bool IsAllowedFileType(string fileName)
    {
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        return ext == ".pdf" || ext == ".jpg" || ext == ".jpeg" || ext == ".png";
    }
}

public class PanUploadRequestValidator : AbstractValidator<PanUploadRequest>
{
    private static readonly long MaxFileSizeBytes = 5 * 1024 * 1024;

    public PanUploadRequestValidator()
    {
        RuleFor(x => x.PanNumber)
            .NotEmpty().WithMessage("PAN number is required.")
            .Matches(@"^[A-Z]{5}[0-9]{4}[A-Z]{1}$").WithMessage("PAN number must be in the format ABCDE1234F.");

        RuleFor(x => x.Document)
            .NotNull().WithMessage("Document is required.")
            .Must(f => f != null && f.Length > 0).WithMessage("Document cannot be empty.")
            .Must(f => f == null || f.Length <= MaxFileSizeBytes).WithMessage("Document must not exceed 5 MB.")
            .Must(f => f == null || IsAllowedFileType(f.FileName))
            .WithMessage("Document must be a PDF, JPG, JPEG, or PNG file.");
    }

    private static bool IsAllowedFileType(string fileName)
    {
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        return ext == ".pdf" || ext == ".jpg" || ext == ".jpeg" || ext == ".png";
    }
}

public class SingleAddressRequestValidator : AbstractValidator<SingleAddressRequest>
{
    public SingleAddressRequestValidator()
    {
        RuleFor(x => x.AddressType)
            .IsInEnum().WithMessage("Invalid address type.");

        RuleFor(x => x.AddressLine1)
            .NotEmpty().WithMessage("Address Line 1 is required.")
            .MaximumLength(200).WithMessage("Address Line 1 cannot exceed 200 characters.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City cannot exceed 100 characters.");

        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State is required.")
            .MaximumLength(100).WithMessage("State cannot exceed 100 characters.");

        RuleFor(x => x.PostalCode)
            .NotEmpty().WithMessage("Postal code is required.")
            .Matches(@"^\d{6}$").WithMessage("Postal code must be exactly 6 digits.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(100).WithMessage("Country cannot exceed 100 characters.");
    }
}
