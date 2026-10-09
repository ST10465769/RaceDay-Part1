namespace RaceDay.Contracts.Auth;

// This class defines exactly what the client (frontend) sends us when they register.
public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    // NOTE: I'm deliberately not putting a "Role" property here. 
    // If I did, someone could just send {"role": "Admin"} and hack the system. 
    // I'll assign the role securely in the controller instead.
}
