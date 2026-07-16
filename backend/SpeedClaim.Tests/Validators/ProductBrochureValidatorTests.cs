using System.Text;
using Microsoft.AspNetCore.Http;
using SpeedClaim.Api.Dtos.Catalog;
using SpeedClaim.Api.Validators;

namespace SpeedClaim.Tests.Validators;

[TestFixture]
public class ProductBrochureValidatorTests
{
    private UploadProductBrochureRequestValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new UploadProductBrochureRequestValidator();
    }

    [Test]
    public async Task Validate_ValidRequest_Passes()
    {
        var result = await _validator.ValidateAsync(Request("4"));

        Assert.That(result.IsValid, Is.True);
    }

    [TestCase("0")]
    [TestCase("-1")]
    [TestCase("v2")]
    [TestCase("1000000")]
    public async Task Validate_InvalidVersion_Fails(string version)
    {
        var result = await _validator.ValidateAsync(Request(version));

        Assert.That(result.Errors.Any(x => x.PropertyName == "Version"), Is.True);
    }

    [Test]
    public async Task Validate_EmptyFile_Fails()
    {
        var request = Request(null);
        request.File = new FormFile(new MemoryStream(), 0, 0, "file", "empty.pdf");

        var result = await _validator.ValidateAsync(request);

        Assert.That(result.Errors.Any(x => x.PropertyName == "File"), Is.True);
    }

    private static UploadProductBrochureRequest Request(string? version)
    {
        var bytes = Encoding.ASCII.GetBytes("%PDF-1.7\nfixture");
        return new UploadProductBrochureRequest
        {
            File = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", "brochure.pdf"),
            EffectiveFrom = new DateOnly(2026, 7, 1),
            Version = version
        };
    }
}
