using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RaceDay.Api.Models;

// Result table from my ERD. The unique index on EnrolmentId makes sure
// one enrolment can only ever have one result (1:1).
[Index(nameof(EnrolmentId), IsUnique = true)]
public class Result
{
    [Key, Column("ResultID")]
    public int ResultId { get; set; }

    [Column("EnrolmentID")]
    public int EnrolmentId { get; set; }

    // TIME in my SQL script, TimeSpan in C#
    [Column(TypeName = "time")]
    public TimeSpan FinishTime { get; set; }

    public int FinishingPosition { get; set; }

    [ForeignKey(nameof(EnrolmentId))]
    public Enrolment Enrolment { get; set; } = null!;
}
