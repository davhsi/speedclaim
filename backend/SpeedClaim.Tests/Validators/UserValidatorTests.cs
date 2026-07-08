using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Http;
using Moq;
using SpeedClaim.Api.Dtos.User;
using SpeedClaim.Api.Models.Enums;
using SpeedClaim.Api.Validators;

namespace SpeedClaim.Tests.Validators;

[TestFixture]
public class AddFamilyMemberRequestValidatorTests
{
    private AddFamilyMemberRequestValidator _validator = null!;

    private static AddFamilyMemberRequest ValidRequest() => new(
        Salutation: Salutation.Mr,
        FirstName: "Arjun",
        LastName: "Kumar",
        DateOfBirth: new DateOnly(1995, 6, 15),
        Gender: Gender.Male,
        Relationship: Relationship.Son,
        IsDependent: true
    );

    [SetUp]
    public void SetUp() => _validator = new AddFamilyMemberRequestValidator();

    [Test]
    public void Valid_Request_Passes()
    {
        _validator.TestValidate(ValidRequest()).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Empty_FirstName_Fails()
    {
        var req = ValidRequest() with { FirstName = "" };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Test]
    public void Empty_LastName_Fails()
    {
        var req = ValidRequest() with { LastName = "" };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Test]
    public void Future_DateOfBirth_Fails()
    {
        var req = ValidRequest() with { DateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddDays(1)) };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.DateOfBirth);
    }

    [Test]
    public void Invalid_Gender_Enum_Fails()
    {
        var req = ValidRequest() with { Gender = (Gender)99 };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Gender);
    }

    [Test]
    public void Invalid_Relationship_Enum_Fails()
    {
        var req = ValidRequest() with { Relationship = (Relationship)99 };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Relationship);
    }
}

[TestFixture]
public class AadhaarUploadRequestValidatorTests
{
    private AadhaarUploadRequestValidator _validator = null!;

    private static Mock<IFormFile> MockFile(string filename = "doc.pdf", long sizeBytes = 1024 * 100)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(filename);
        mock.Setup(f => f.Length).Returns(sizeBytes);
        return mock;
    }

    [SetUp]
    public void SetUp() => _validator = new AadhaarUploadRequestValidator();

    [Test]
    public void Valid_Aadhaar_Request_Passes()
    {
        var req = new AadhaarUploadRequest(null, "123456789012", MockFile().Object);
        _validator.TestValidate(req).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Aadhaar_With_11_Digits_Fails()
    {
        var req = new AadhaarUploadRequest(null, "12345678901", MockFile().Object);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.AadhaarNumber);
    }

    [Test]
    public void Aadhaar_With_Letters_Fails()
    {
        var req = new AadhaarUploadRequest(null, "12345678901A", MockFile().Object);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.AadhaarNumber);
    }

    [Test]
    public void Null_Document_Fails()
    {
        var req = new AadhaarUploadRequest(null, "123456789012", null!);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Document);
    }

    [Test]
    public void File_Exceeding_5MB_Fails()
    {
        var bigFile = MockFile("doc.pdf", 6 * 1024 * 1024);
        var req = new AadhaarUploadRequest(null, "123456789012", bigFile.Object);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Document);
    }

    [Test]
    public void Disallowed_File_Type_Fails()
    {
        var exeFile = MockFile("document.exe");
        var req = new AadhaarUploadRequest(null, "123456789012", exeFile.Object);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Document);
    }
}

[TestFixture]
public class PanUploadRequestValidatorTests
{
    private PanUploadRequestValidator _validator = null!;

    private static Mock<IFormFile> MockFile(string filename = "doc.pdf", long sizeBytes = 1024 * 100)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(filename);
        mock.Setup(f => f.Length).Returns(sizeBytes);
        return mock;
    }

    [SetUp]
    public void SetUp() => _validator = new PanUploadRequestValidator();

    [Test]
    public void Valid_Pan_Request_Passes()
    {
        var req = new PanUploadRequest(null, "ABCDE1234F", MockFile().Object);
        _validator.TestValidate(req).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Invalid_Pan_Format_Fails()
    {
        var req = new PanUploadRequest(null, "ABCDE12345", MockFile().Object);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.PanNumber);
    }

    [Test]
    public void Null_Document_Fails()
    {
        var req = new PanUploadRequest(null, "ABCDE1234F", null!);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Document);
    }

    [Test]
    public void File_Exceeding_5MB_Fails()
    {
        var bigFile = MockFile("doc.pdf", 6 * 1024 * 1024);
        var req = new PanUploadRequest(null, "ABCDE1234F", bigFile.Object);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Document);
    }

    [Test]
    public void Disallowed_File_Type_Fails()
    {
        var exeFile = MockFile("document.exe");
        var req = new PanUploadRequest(null, "ABCDE1234F", exeFile.Object);
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Document);
    }
}

[TestFixture]
public class SingleAddressRequestValidatorTests
{
    private SingleAddressRequestValidator _validator = null!;

    private static SingleAddressRequest ValidRequest() => new(
        AddressType: AddressType.Current,
        AddressLine1: "42 MG Road",
        AddressLine2: null,
        City: "Bengaluru",
        State: "Karnataka",
        PostalCode: "560001",
        Country: "India",
        IsSameAsPermanent: false
    );

    [SetUp]
    public void SetUp() => _validator = new SingleAddressRequestValidator();

    [Test]
    public void Valid_Request_Passes()
    {
        _validator.TestValidate(ValidRequest()).ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Empty_AddressLine1_Fails()
    {
        var req = ValidRequest() with { AddressLine1 = "" };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.AddressLine1);
    }

    [Test]
    public void Empty_City_Fails()
    {
        var req = ValidRequest() with { City = "" };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.City);
    }

    [Test]
    public void Empty_State_Fails()
    {
        var req = ValidRequest() with { State = "" };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.State);
    }

    [Test]
    public void Pincode_With_5_Digits_Fails()
    {
        var req = ValidRequest() with { PostalCode = "56000" };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.PostalCode);
    }

    [Test]
    public void Pincode_With_Letters_Fails()
    {
        var req = ValidRequest() with { PostalCode = "5600AB" };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.PostalCode);
    }

    [Test]
    public void Empty_Country_Fails()
    {
        var req = ValidRequest() with { Country = "" };
        _validator.TestValidate(req).ShouldHaveValidationErrorFor(x => x.Country);
    }
}
