using Microsoft.EntityFrameworkCore;
using RaceDay.API.Models;
namespace RaceDay.API.Data;
public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(
        DbContextOptions<RaceDayDbContext> options)
        : base(options)
    {
    }
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Table names
        modelBuilder.Entity<Category>().ToTable("Category");
        modelBuilder.Entity<Enrolment>().ToTable("Enrolment");
        modelBuilder.Entity<Event>().ToTable("Event");
        modelBuilder.Entity<Result>().ToTable("Result");
        modelBuilder.Entity<Role>().ToTable("Role");
        modelBuilder.Entity<User>().ToTable("User");
        // Primary keys
        modelBuilder.Entity<Category>()
            .HasKey(c => c.CategoryId);
        modelBuilder.Entity<Enrolment>()
            .HasKey(e => e.EnrolmentId);
        modelBuilder.Entity<Event>()
            .HasKey(e => e.EventId);
        modelBuilder.Entity<Result>()
            .HasKey(r => r.ResultId);
        modelBuilder.Entity<Role>()
            .HasKey(r => r.RoleId);
        modelBuilder.Entity<User>()
            .HasKey(u => u.UserId);
        // Decimal precision
        modelBuilder.Entity<Event>()
            .Property(e => e.Distance)
            .HasPrecision(6, 2);
        modelBuilder.Entity<Category>()
            .Property(c => c.CategoryDistance)
            .HasPrecision(6, 2);
        // Category -> Event
        modelBuilder.Entity<Category>()
            .HasOne(c => c.Event)
            .WithMany(e => e.Categories)
            .HasForeignKey(c => c.EventId)
            .HasConstraintName("FK_Category_Event");
        // Enrolment -> Category
        modelBuilder.Entity<Enrolment>()
            .HasOne(e => e.Category)
            .WithMany()
            .HasForeignKey(e => e.CategoryId)
            .HasConstraintName("FK_Enrolment_Category");
        // Enrolment -> Event
        modelBuilder.Entity<Enrolment>()
            .HasOne(e => e.Event)
            .WithMany(ev => ev.Enrolments)
            .HasForeignKey(e => e.EventId)
            .HasConstraintName("FK_Enrolment_Event");
        // Enrolment -> User (Participant)
        modelBuilder.Entity<Enrolment>()
            .HasOne(e => e.Participant)
            .WithMany()
            .HasForeignKey(e => e.ParticipantId)
            .HasPrincipalKey(u => u.UserId)
            .HasConstraintName("FK_Enrolment_Participant");
        // Event -> User (Organiser)
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Organiser)
            .WithMany()
            .HasForeignKey(e => e.OrganiserId)
            .HasPrincipalKey(u => u.UserId)
            .HasConstraintName("FK_Event_Organiser");
        // Result -> Enrolment
        modelBuilder.Entity<Result>()
            .HasOne(r => r.Enrolment)
            .WithMany()
            .HasForeignKey(r => r.EnrolmentId)
            .HasConstraintName("FK_Result_Enrolment");
        // User -> Role
        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany(r => r.Users)
            .HasForeignKey(u => u.RoleId)
            .HasConstraintName("FK_User_Role");
    }
}