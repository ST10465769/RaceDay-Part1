namespace RaceDay.Contracts.Auth;

// This is what we send back to the client after a successful registration.
public class UserResponse
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    // I am NOT including the PasswordHash here. Never send sensitive data back to the client!
}
