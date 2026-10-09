using System.Security.Cryptography;

namespace RaceDay.Api.Services;

// Hashes passwords with PBKDF2, which is built into .NET.
// The plain password is never stored in the Users table, only this hash.
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;        // 16 random bytes of salt
    private const int HashSize = 32;        // 32 bytes (256 bits) for the hash
    private const int Iterations = 100_000; // more iterations make guessing slower

    public string Hash(string password)
    {
        // A new random salt for every user, so two people with the same
        // password still end up with different hashes
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

        // I store everything in one string: iterations.salt.hash
        // That way Verify can read the salt back out later
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hash)
    {
        // Split the stored string back into its three parts
        string[] parts = hash.Split('.');
        if (parts.Length != 3)
        {
            return false; // not a hash made by this class
        }

        int iterations = int.Parse(parts[0]);
        byte[] salt = Convert.FromBase64String(parts[1]);
        byte[] storedHash = Convert.FromBase64String(parts[2]);

        // Hash the password that was just typed in, using the same salt
        byte[] attempt = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, iterations, HashAlgorithmName.SHA256, storedHash.Length);

        // FixedTimeEquals compares in constant time so timing can't leak information
        return CryptographicOperations.FixedTimeEquals(attempt, storedHash);
    }
}