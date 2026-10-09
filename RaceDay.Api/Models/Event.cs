using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

// The three event types allowed by the brief
public static class EventTypes
{
    public const string Run = "Run";
    public const string Walk = "Walk";
    public const string Cycle = "Cycle";
    public static readonly string[] All = { Run, Walk, Cycle };
}

// Event table from my ERD. Each event belongs to one Organiser.
[Table("Event")]
public class Event
{
    [Key, Column("EventID")]
    public int EventId { get; set; }

    [Required, StringLength(100)]
    public string EventName { get; set; } = string.Empty;

    // TEXT in my SQL script, so no length limit here
    public string? Description { get; set; }

    // DATE in my SQL script (no time part)
    [Column(TypeName = "date")]
    public DateTime EventDate { get; set; }

    [Required, StringLength(200)]
    public string Location { get; set; } = string.Empty;

    // DECIMAL(5,2) in my SQL script, e.g. 42.20 km
    [Column(TypeName = "decimal(5,2)")]
    public decimal Distance { get; set; }

    // Run, Walk or Cycle
    [Required, StringLength(10)]
    public string EventType { get; set; } = string.Empty;

    [StringLength(500)]
    public string? BannerImageUrl { get; set; }   // used in Part 3

    public DateTime DateCreated { get; set; } = DateTime.UtcNow;

    // Foreign key to the Organiser who owns this event
    [Column("OrganiserID")]
    public int OrganiserId { get; set; }

    [ForeignKey(nameof(OrganiserId))]
    public AppUser Organiser { get; set; } = null!;

    public List<Category> Categories { get; set; } = new();
    public List<Enrolment> Enrolments { get; set; } = new();
}