using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaceDay.Api.Models;

// Category table from my ERD, e.g. Under 30, Senior, 10km.
// Every category belongs to one event.
public class Category
{
    [Key, Column("CategoryID")]
    public int CategoryId { get; set; }

    [Required, StringLength(50)]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    [Column("EventID")]
    public int EventId { get; set; }

    [ForeignKey(nameof(EventId))]
    public Event Event { get; set; } = null!;

    public List<Enrolment> Enrolments { get; set; } = new();
}