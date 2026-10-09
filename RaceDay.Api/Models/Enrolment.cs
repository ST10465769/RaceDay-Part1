using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RaceDay.Api.Models;

// Allowed enrolment statuses (ENUM in my SQL script)
public static class EnrolmentStatuses
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
    public static readonly string[] All = { Pending, Confirmed, Cancelled };
}

// Enrolment links a Participant, an Event and the Category they chose.
// The unique index matches "unique_enrolment" in my SQL script,
// so a participant can only enter the same event once.
[Index(nameof(ParticipantId), nameof(EventId), IsUnique = true)]
[Index(nameof(ParticipantId), nameof(EventId), IsUnique = true)]
[Table("Enrolment")]
public class Enrolment
{
    [Key, Column("EnrolmentID")]
    public int EnrolmentId { get; set; }

    [Column("ParticipantID")]
    public int ParticipantId { get; set; }

    [Column("EventID")]
    public int EventId { get; set; }

    [Column("CategoryID")]
    public int CategoryId { get; set; }

    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;

    // New enrolments start as Pending
    [StringLength(10)]
    public string Status { get; set; } = EnrolmentStatuses.Pending;

    [ForeignKey(nameof(ParticipantId))]
    public AppUser Participant { get; set; } = null!;

    [ForeignKey(nameof(EventId))]
    public Event Event { get; set; } = null!;

    [ForeignKey(nameof(CategoryId))]
    public Category Category { get; set; } = null!;

    // One enrolment has zero or one result (1:1 in my ERD)
    public Result? Result { get; set; }
}