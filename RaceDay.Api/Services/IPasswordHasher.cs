namespace RaceDay.Api.Services;

// I use an interface so the controller doesn't depend on one specific hashing method,
// and so my unit tests can swap it out later if needed
public interface IPasswordHasher
{
    // Turns the plain password into a hash that is safe to store in the database
    string Hash(string password);

    // Checks if a plain password matches a hash that was stored earlier
    bool Verify(string password, string hash);
}

