using System;
using System.Security.Cryptography;
using System.Text;

namespace TripleDetection.Application.Services
{

public interface IPasswordHashService
{
    string GenerateSalt();
    string ComputeHash(string salt, string password);
    bool VerifyPassword(string enteredPassword, string storedSalt, string storedHash);
    bool IsLegacyPlainText(string storedHash);
}

public class PasswordHashService : IPasswordHashService
{
    private const int SaltSize = 16;

    public string GenerateSalt()
    {
        var bytes = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes);
    }

    public string ComputeHash(string salt, string password)
    {
        var combined = salt + password;
        using (var sha256 = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(combined);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }

    public bool VerifyPassword(string enteredPassword, string storedSalt, string storedHash)
    {
        if (string.IsNullOrEmpty(storedSalt) || string.IsNullOrEmpty(storedHash)) return false;
        return ComputeHash(storedSalt, enteredPassword) == storedHash;
    }

    public bool IsLegacyPlainText(string storedHash)
    {
        if (string.IsNullOrEmpty(storedHash)) return true;
        return storedHash.Length < 32;
    }
}
}