using System.Security.Cryptography;

namespace aspnetproject.BusinessLogic.Services.Helpers;

public class PasswordHasher
{
    private const int SaltLength = 32;
    private const int HashLength = 32;
    private const int Iterations = 100_000;
    
    public (string hash, string salt) HashPassword(string password)
    {
        byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltLength);
        byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            Iterations,
            HashAlgorithmName.SHA256,
            HashLength);
        return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
    }

    public bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        byte[] saltBytes = Convert.FromBase64String(storedSalt);

        byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            Iterations,
            HashAlgorithmName.SHA256,
            HashLength);

        bool isValid = CryptographicOperations.FixedTimeEquals(hashBytes, Convert.FromBase64String(storedHash));
        return isValid;
    }
}
