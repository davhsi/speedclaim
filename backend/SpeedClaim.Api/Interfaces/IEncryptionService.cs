namespace SpeedClaim.Api.Interfaces;

public interface IEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
    string Mask(string plaintext);
}
