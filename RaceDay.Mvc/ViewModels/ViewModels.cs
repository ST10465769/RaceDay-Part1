using System.ComponentModel.DataAnnotations;
using RaceDay.Contracts;

namespace RaceDay.Mvc.ViewModels;

public class LoginVm
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, DataType(DataType.Password)] public string Password { get; set; } = string.Empty;
}

public class RegisterVm
{
    [Required, StringLength(50), Display(Name = "First name")] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(50), Display(Name = "Last name")] public string LastName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(100)] public string Email { get; set; } = string.Empty;
    [Phone, StringLength(20), Display(Name = "Phone number (optional)")] public string? PhoneNumber { get; set; }
    [Required, MinLength(6), DataType(DataType.Password)] public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required, Display(Name = "I am registering as")] public string Role { get; set; } = "Participant";
}

// ---------- Organiser ----------
public class OrganiserDashboardVm
{
    public string DisplayName { get; set; } = string.Empty;
    public List<EventDto> Events { get; set; } = new();
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
    public List<EventDto> Upcoming => Events.Where(e => e.EventDate >= DateTime.Today).OrderBy(e => e.EventDate).ToList();
}

// One form model for both creating and editing an event
public class EventFormVm
{
    public int EventId { get; set; }

    [Required, StringLength(100), Display(Name = "Event name")] public string EventName { get; set; } = string.Empty;
    [Required, StringLength(500)] public string Description { get; set; } = string.Empty;
    [Required, DataType(DataType.Date), Display(Name = "Date")] public DateTime EventDate { get; set; } = DateTime.Today.AddDays(30);
    [Required, StringLength(200)] public string Location { get; set; } = string.Empty;
    [Range(0.1, 999.99), Display(Name = "Distance (km)")] public decimal Distance { get; set; } = 10;
    [Required, Display(Name = "Event type")] public string EventType { get; set; } = "Run";
    public string? BannerImageUrl { get; set; }
}

public class CategoriesPageVm
{
    public List<EventDto> Events { get; set; } = new();
    public int? EventId { get; set; }
    public List<CategoryDto> Categories { get; set; } = new();
}

public class EnrolmentsPageVm
{
    public List<EventDto> Events { get; set; } = new();
    public int? EventId { get; set; }
    public List<EnrolmentDto> Enrolments { get; set; } = new();
}

public class ResultsPageVm
{
    public List<EventDto> Events { get; set; } = new();
    public int? EventId { get; set; }
    public List<EnrolmentDto> Enrolments { get; set; } = new();
    public List<ResultDto> Results { get; set; } = new();
}

// ---------- Participant ----------
public class ParticipantEventsVm
{
    public List<EventDto> Events { get; set; } = new();
    public string? Type { get; set; }
    public string? Search { get; set; }
}

public class EventDetailsVm
{
    public EventDto Event { get; set; } = new();
    public EnrolmentDto? MyEnrolment { get; set; }
}

public class ProfileVm
{
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    [Required, StringLength(50), Display(Name = "First name")] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(50), Display(Name = "Last name")] public string LastName { get; set; } = string.Empty;
    [Phone, StringLength(20), Display(Name = "Phone number")] public string? PhoneNumber { get; set; }
    public string? ProfilePictureUrl { get; set; }
}