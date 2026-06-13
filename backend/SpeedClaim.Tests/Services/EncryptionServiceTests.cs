using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using SpeedClaim.Api.Services;

namespace SpeedClaim.Tests.Services;

[TestFixture]
public class EncryptionServiceTests
{
    private EncryptionService _service = null!;

    // 32 zero-bytes encoded as base64 — a valid 256-bit key for testing only
    private const string TestKeyBase64 = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    private static IConfiguration BuildConfig(string? key = TestKeyBase64)
    {
        var values = new Dictionary<string, string?>();
        if (key != null)
            values["SecuritySettings:EncryptionKey"] = key;
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [SetUp]
    public void SetUp()
    {
        _service = new EncryptionService(BuildConfig());
    }

    // --- Constructor ---

    [Test]
    public void Constructor_Throws_When_Key_Missing()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new EncryptionService(BuildConfig(key: null)));
    }

    [Test]
    public void Constructor_Throws_When_Key_Not_32_Bytes()
    {
        // 16-byte key (too short)
        var shortKey = Convert.ToBase64String(new byte[16]);
        Assert.Throws<InvalidOperationException>(() =>
            new EncryptionService(BuildConfig(shortKey)));
    }

    // --- Encrypt ---

    [Test]
    public void Encrypt_Returns_Base64_String()
    {
        var result = _service.Encrypt("123456789012");
        Assert.That(() => Convert.FromBase64String(result), Throws.Nothing);
    }

    [Test]
    public void Encrypt_Produces_Different_Ciphertext_Each_Call()
    {
        var ct1 = _service.Encrypt("123456789012");
        var ct2 = _service.Encrypt("123456789012");
        Assert.That(ct1, Is.Not.EqualTo(ct2));
    }

    [Test]
    public void Encrypt_Output_Is_At_Least_IV_Plus_One_Block()
    {
        var result = _service.Encrypt("hi");
        var bytes = Convert.FromBase64String(result);
        // 16-byte IV + at least 16-byte AES block
        Assert.That(bytes.Length, Is.GreaterThanOrEqualTo(32));
    }

    // --- Decrypt ---

    [Test]
    public void Decrypt_Roundtrip_Aadhaar()
    {
        const string original = "123456789012";
        var encrypted = _service.Encrypt(original);
        var decrypted = _service.Decrypt(encrypted);
        Assert.That(decrypted, Is.EqualTo(original));
    }

    [Test]
    public void Decrypt_Roundtrip_Pan()
    {
        const string original = "ABCDE1234F";
        var encrypted = _service.Encrypt(original);
        var decrypted = _service.Decrypt(encrypted);
        Assert.That(decrypted, Is.EqualTo(original));
    }

    [Test]
    public void Decrypt_Roundtrip_Unicode()
    {
        const string original = "Aadhaar: नमस्ते";
        var encrypted = _service.Encrypt(original);
        var decrypted = _service.Decrypt(encrypted);
        Assert.That(decrypted, Is.EqualTo(original));
    }

    [Test]
    public void Decrypt_Falls_Back_On_Invalid_Ciphertext()
    {
        // Raw plaintext stored before encryption was introduced
        const string raw = "123456789012";
        var result = _service.Decrypt(raw);
        Assert.That(result, Is.EqualTo(raw));
    }

    [Test]
    public void Decrypt_Falls_Back_On_Garbage_Base64()
    {
        const string garbage = "bm90LXJlYWwtY2lwaGVydGV4dA=="; // "not-real-ciphertext" in base64
        var result = _service.Decrypt(garbage);
        Assert.That(result, Is.EqualTo(garbage));
    }

    // --- Mask ---

    [Test]
    public void Mask_Shows_Last_4_Chars_For_Aadhaar()
    {
        var result = _service.Mask("123456789012");
        Assert.That(result, Is.EqualTo("XXXXXXXX9012"));
    }

    [Test]
    public void Mask_Shows_Last_4_Chars_For_Pan()
    {
        var result = _service.Mask("ABCDE1234F");
        Assert.That(result, Is.EqualTo("XXXXXX234F"));
    }

    [Test]
    public void Mask_Short_String_All_X()
    {
        Assert.That(_service.Mask("AB"), Is.EqualTo("XX"));
        Assert.That(_service.Mask("ABCD"), Is.EqualTo("XXXX"));
    }

    [Test]
    public void Mask_Exactly_5_Chars_Shows_Last_4()
    {
        var result = _service.Mask("ABCDE");
        Assert.That(result, Is.EqualTo("XBCDE"));
    }

    [Test]
    public void Mask_Does_Not_Reveal_Middle_Digits()
    {
        var result = _service.Mask("123456789012");
        Assert.That(result, Does.Not.Contain("5678"));
    }

    // --- Encrypt + Decrypt consistency across multiple values ---

    [Test]
    public void Multiple_Values_Encrypt_Decrypt_Independently()
    {
        var pairs = new[] { "111122223333", "PQRST9876W", "A1234567" };
        var encrypted = Array.ConvertAll(pairs, _service.Encrypt);
        for (var i = 0; i < pairs.Length; i++)
            Assert.That(_service.Decrypt(encrypted[i]), Is.EqualTo(pairs[i]));
    }
}
