namespace RaceDay.Api.Services;

// Interface for our password hasher. This allows us to use Dependency Injection.
public interface IPasswordHasher
{
    // Takes a plain text password and returns a secure hash
    string Hash(string password);

    // Checks if a plain text password matches the stored hash (for login later)
    bool Verify(string password, string hash);
}
