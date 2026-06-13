using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using SpeedClaim.Api.Interfaces;

namespace SpeedClaim.Api.Services;

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public EncryptionService(IConfiguration configuration)
    {
        var keyBase64 = configuration["SecuritySettings:EncryptionKey"]
            ?? throw new InvalidOperationException("SecuritySettings:EncryptionKey is not configured.");
        _key = Convert.FromBase64String(keyBase64);
        if (_key.Length != 32)
            throw new InvalidOperationException("Encryption key must be 256 bits (32 bytes).");
    }

    public string Encrypt(string plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipherBytes = encryptor.TransformFinalBlock(plaintextBytes, 0, plaintextBytes.Length);

        // IV (16 bytes) prepended to ciphertext, then base64-encoded as a single blob
        var result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
        return Convert.ToBase64String(result);
    }

    public string Decrypt(string ciphertext)
    {
        try
        {
            var fullBytes = Convert.FromBase64String(ciphertext);

            using var aes = Aes.Create();
            aes.Key = _key;

            const int ivLength = 16;
            var iv = new byte[ivLength];
            var cipherBytes = new byte[fullBytes.Length - ivLength];
            Buffer.BlockCopy(fullBytes, 0, iv, 0, ivLength);
            Buffer.BlockCopy(fullBytes, ivLength, cipherBytes, 0, cipherBytes.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            // Value was stored before encryption was introduced — return as-is
            return ciphertext;
        }
    }

    public string Mask(string plaintext)
    {
        if (plaintext.Length <= 4)
            return new string('X', plaintext.Length);
        return new string('X', plaintext.Length - 4) + plaintext[^4..];
    }
}
