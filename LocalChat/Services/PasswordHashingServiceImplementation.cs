using System.Security.Cryptography;
using LocalChat.Services.Interfaces;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace LocalChat.Services
{
    public class PasswordHasher : IPasswordHasher
    {
        private readonly int saltSize = 16; // 128 bit
        private readonly int keySize = 32; // 256 bit
        private readonly int iterations = 10000; // PBKDF2 iteration count

        public string HashPassword(string password)
        {
            // Generate a salt
            var salt = new byte[saltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Derive the key (password hash)
            var hashed = KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: iterations,
                numBytesRequested: keySize
            );

            // Combine salt and hash
            var hashBytes = new byte[saltSize + keySize];
            Array.Copy(salt, 0, hashBytes, 0, saltSize);
            Array.Copy(hashed, 0, hashBytes, saltSize, keySize);

            // Convert to base64 for storage
            return Convert.ToBase64String(hashBytes);
        }

        public bool VerifyPassword(string hashedPassword, string providedPassword)
        {
            // Convert hash string back to bytes
            var hashBytes = Convert.FromBase64String(hashedPassword);

            // Extract salt
            var salt = new byte[saltSize];
            Array.Copy(hashBytes, 0, salt, 0, saltSize);

            // Extract stored hash
            var storedHash = new byte[keySize];
            Array.Copy(hashBytes, saltSize, storedHash, 0, keySize);

            // Hash the provided password using the same salt and params
            var hashedProvidedPassword = KeyDerivation.Pbkdf2(
                password: providedPassword,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: iterations,
                numBytesRequested: keySize
            );

            // Compare hashes
            return CryptographicOperations.FixedTimeEquals(storedHash, hashedProvidedPassword);
        }
    }
}