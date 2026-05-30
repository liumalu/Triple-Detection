using System;
using System.Security.Cryptography;
using System.Text;

namespace TripleDetection.Services
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
            if (string.IsNullOrEmpty(storedSalt) || string.IsNullOrEmpty(storedHash))
                return false;
            var hash = ComputeHash(storedSalt, enteredPassword);
            return hash == storedHash;
        }

        public bool IsLegacyPlainText(string storedHash)
        {
            // If storedHash is short and not a valid base64 string of 32 bytes (SHA256 output),
            // treat it as a legacy plain text password
            if (string.IsNullOrEmpty(storedHash))
                return true;
            // SHA256 produces 32 bytes = 44 chars in base64
            if (storedHash.Length < 32)
                return true;
            return false;
        }
    }
}