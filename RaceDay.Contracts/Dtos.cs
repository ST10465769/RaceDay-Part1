using System.ComponentModel.DataAnnotations;

namespace RaceDay.Contracts;

// DTOs (Data Transfer Objects) only carry data in and out of the API.
// They are not database tables. The models in RaceDay.Api are the tables.
// I put them in the Contracts project so the MVC app in Part 3 can reuse them.
// The field names follow my Part 1 API endpoint plan.

// ---------- Authentication ----------

// Data the user sends when creating an account
public class RegisterRequest
{
    // [Required] means the API rejects the request (400) if this is empty
    [Required, StringLength(50)] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(50)] public string LastName { get; set; } = string.Empty;

    // [EmailAddress] checks the text looks like a real email
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;

    // Plain password from the user. The API hashes it before saving,
    // so the plain text is never stored in the database.
    [Required, MinLength(6)] public string Password { get; set; } = string.Empty;

    // The user picks their role here: "Organiser" or "Participant"
    [Required] public string Role { get; set; } = string.Empty;

    // Phone number is in my Part 1 register request body, so I kept it (optional)
    [Phone, StringLength(20)] public string? PhoneNumber { get; set; }
}

// Data the user sends when logging in
public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

// What the API sends back after a successful register or login.
// There is no password here on purpose, so nothing sensitive is returned.
public class AuthResponse
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

// ---------- Profile ----------

// What the API returns for GET /api/users/profile (no password or hash)
public class ProfileDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string Role { get; set; } = string.Empty;
}

// Body for PUT /api/users/profile.
// There is no UserId here because the API takes it from the session,
// so a user can only update their own account.
public class UpdateProfileRequest
{
    [Required, StringLength(50)] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(50)] public string LastName { get; set; } = string.Empty;
    [Phone, StringLength(20)] public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

// ---------- Events ----------

// Body for POST /api/events.
// It covers what the brief says an event must have:
// name, description, date, location, distance and event type.
public class CreateEventRequest
{
    [Required, StringLength(100)] public string EventName { get; set; } = string.Empty;
    [Required, StringLength(500)] public string Description { get; set; } = string.Empty;
    [Required] public DateTime EventDate { get; set; }
    [Required, StringLength(200)] public string Location { get; set; } = string.Empty;

    // Distance in km. [Range] stops zero or negative values.
    [Range(0.1, 999.99)] public decimal Distance { get; set; }

    // Must be Run, Walk or Cycle. I check this in the controller.
    [Required] public string EventType { get; set; } = string.Empty;

    // Optional, because the banner image upload only comes in Part 3
    public string? BannerImageUrl { get; set; }
}

// Updating an event needs the same fields as creating one,
// so I inherit from CreateEventRequest instead of copying the code
public class UpdateEventRequest : CreateEventRequest { }

// What the API returns for an event
public class EventDto
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? BannerImageUrl { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;

    // My plan says GET /api/events/{id} returns the categories and enrolment count
    public int EnrolmentCount { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();
}

// ---------- Categories ----------

// Body for POST and PUT on categories (categoryName and description in my plan).
// The EventId comes from the URL (/api/events/{eventId}/categories).
public class CreateCategoryRequest
{
    // e.g. Under 20, Senior, 10km, 21km
    [Required, StringLength(50)] public string CategoryName { get; set; } = string.Empty;
    [StringLength(200)] public string? Description { get; set; }
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// ---------- Enrolments ----------

// A Participant sends this to enter an event.
// The ParticipantId is not here because it comes from the session.
public class CreateEnrolmentRequest
{
    [Required] public int EventId { get; set; }
    [Required] public int CategoryId { get; set; }
}

// An Organiser uses this to change the status of an enrolment
public class UpdateEnrolmentStatusRequest
{
    // Pending, Confirmed or Cancelled
    [Required] public string Status { get; set; } = string.Empty;
}

// What the API returns for an enrolment.
// It includes the event, category and participant names so the front end
// doesn't need extra calls to look them up.
public class EnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }

}

// ---------- Results ----------

// An Organiser sends this to record a participant's result.
// It links to the Enrolment, which already knows the participant and event.
public class CreateResultRequest
{
    [Required] public int EnrolmentId { get; set; }

    // TimeSpan is the right type for a race time (e.g. 01:45:30)
    [Required] public TimeSpan FinishTime { get; set; }

    // Position has to be 1 or higher
    [Range(1, int.MaxValue)] public int FinishingPosition { get; set; }
}

// What the API returns when someone views a result
public class ResultDto
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
    public TimeSpan FinishTime { get; set; }
    public int FinishingPosition { get; set; }
}

// ---------- Part 3 (image uploads) ----------

// For Part 3: returns the Azure Blob Storage link after an image upload
public class UploadResponse
{
    public string Url { get; set; } = string.Empty;
}