namespace RaceDay.Api.Services;

// This class implements the interface using BCrypt.
public class PasswordHasher : IPasswordHasher
{
    // BCrypt automatically adds a "salt" and hashes the password securely.
    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password);

    // Verifies a login attempt against the stored hash.
    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);
}
