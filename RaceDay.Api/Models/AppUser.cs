using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace RaceDay.Api.Models;

// Role names in one place so I don't mistype the strings in the controllers
public static class UserRoles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";
}

// This is the User table from my Part 1 ERD.
// The class is called AppUser because "User" is a confusing name in C#,
// but [Table("User")] keeps the table name the same as my ERD.
[Table("User")]
[Index(nameof(Email), IsUnique = true)] // two accounts can't share an email
public class AppUser
{
    [Key, Column("UserID")]
    public int UserId { get; set; }

    [Required, StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Email { get; set; } = string.Empty;

    // Only the hash is stored, never the real password
    [Required, StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    // "Organiser" or "Participant" (ENUM in my SQL script)
    [Required, StringLength(20)]
    public string Role { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    public DateTime DateRegistered { get; set; } = DateTime.UtcNow;

    [StringLength(500)]
    public string? ProfilePictureUrl { get; set; }

    // Navigation properties: one user has many of these (1:M in my ERD)
    public List<Event> Events { get; set; } = new();              // events an Organiser created
    public List<Enrolment> Enrolments { get; set; } = new();      // events a Participant entered
    public List<Notification> Notifications { get; set; } = new();
}