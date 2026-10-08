using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

// The DbContext is the bridge between my C# models and the SQL Server database.
// Each DbSet below becomes a table when I run the migration.
public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
    {
    }

    // One DbSet per entity in my Part 1 ERD
    public DbSet<AppUser> Users { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Enrolment> Enrolments { get; set; }
    public DbSet<Result> Results { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    // OnModelCreating is where I set the relationships and delete behaviour
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User (Organiser) 1:M Event
        // Deleting an Organiser deletes their events (ON DELETE CASCADE in my SQL script)
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Organiser)
            .WithMany(u => u.Events)
            .HasForeignKey(e => e.OrganiserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Event 1:M Category
        // Deleting an event deletes its categories
        modelBuilder.Entity<Category>()
            .HasOne(c => c.Event)
            .WithMany(e => e.Categories)
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Event 1:M Enrolment
        // Deleting an event deletes its enrolments (my endpoint plan says DELETE event
        // also removes the categories and enrolments)
        modelBuilder.Entity<Enrolment>()
            .HasOne(e => e.Event)
            .WithMany(ev => ev.Enrolments)
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // User (Participant) 1:M Enrolment
        // SQL Server doesn't allow "multiple cascade paths" to the same table.
        // Enrolment already cascades from Event (and Event cascades from User),
        // so I use Restrict here and for Category below to avoid that error.
        modelBuilder.Entity<Enrolment>()
            .HasOne(e => e.Participant)
            .WithMany(u => u.Enrolments)
            .HasForeignKey(e => e.ParticipantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Category 1:M Enrolment (Restrict for the same reason as above)
        modelBuilder.Entity<Enrolment>()
            .HasOne(e => e.Category)
            .WithMany(c => c.Enrolments)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Enrolment 1:1 Result (an enrolment has zero or one result)
        // Deleting an enrolment deletes its result
        modelBuilder.Entity<Result>()
            .HasOne(r => r.Enrolment)
            .WithOne(e => e.Result)
            .HasForeignKey<Result>(r => r.EnrolmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // User 1:M Notification
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}