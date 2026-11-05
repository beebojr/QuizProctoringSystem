using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Domain.Entities;

namespace QPS.Data;

public class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Semester> Semesters => Set<Semester>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizLocation> QuizLocations => Set<QuizLocation>();
    public DbSet<ProctorAssignment> ProctorAssignments => Set<ProctorAssignment>();
    public DbSet<TAScheduleSlot> TAScheduleSlots => Set<TAScheduleSlot>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Excuse> Excuses => Set<Excuse>();
    public DbSet<SystemSettings> SystemSettings => Set<SystemSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Deletes are real deletes, except users: a removed user is flagged IsDeleted so past
        // proctoring history keeps its name. Queries filter deleted users explicitly.

        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            e.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
            e.Property(u => u.Department).HasMaxLength(50).IsRequired();
            e.Ignore(u => u.FullName);
        });

        modelBuilder.Entity<Course>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(200).IsRequired();
            e.Property(c => c.NormalizedName).HasMaxLength(200).IsRequired();
            e.Property(c => c.Code).HasMaxLength(30);
            e.Property(c => c.Department).HasMaxLength(50).IsRequired();
            e.HasIndex(c => new { c.Department, c.NormalizedName }).IsUnique();
        });

        modelBuilder.Entity<Semester>(e =>
        {
            e.Property(s => s.Name).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Room>(e =>
        {
            e.Property(r => r.Name).HasMaxLength(50).IsRequired();
            e.Property(r => r.NormalizedName).HasMaxLength(50).IsRequired();
            e.HasIndex(r => r.NormalizedName).IsUnique();
        });

        modelBuilder.Entity<Quiz>(e =>
        {
            e.HasOne(q => q.Course).WithMany().HasForeignKey(q => q.CourseId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(q => q.Semester).WithMany().HasForeignKey(q => q.SemesterId).OnDelete(DeleteBehavior.Restrict);
            e.Property(q => q.Notes).HasMaxLength(500);
            e.HasIndex(q => new { q.SemesterId, q.QuizDate });
        });

        modelBuilder.Entity<QuizLocation>(e =>
        {
            e.HasOne(l => l.Quiz).WithMany(q => q.Locations).HasForeignKey(l => l.QuizId).OnDelete(DeleteBehavior.Cascade);
            e.Property(l => l.RoomName).HasMaxLength(50).IsRequired();
            e.Property(l => l.Group).HasMaxLength(40);
        });

        modelBuilder.Entity<ProctorAssignment>(e =>
        {
            e.HasOne(a => a.QuizLocation).WithMany(l => l.Assignments).HasForeignKey(a => a.QuizLocationId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);
            e.Property(a => a.OverrideReason).HasMaxLength(300);
            e.HasIndex(a => new { a.QuizLocationId, a.UserId }).IsUnique();
            e.HasIndex(a => a.UserId);
        });

        modelBuilder.Entity<TAScheduleSlot>(e =>
        {
            e.HasOne(s => s.User).WithMany().HasForeignKey(s => s.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Semester).WithMany().HasForeignKey(s => s.SemesterId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Course).WithMany().HasForeignKey(s => s.CourseId).OnDelete(DeleteBehavior.SetNull);
            e.Property(s => s.CourseName).HasMaxLength(200).IsRequired();
            e.Property(s => s.Room).HasMaxLength(50).IsRequired();
            e.HasIndex(s => new { s.UserId, s.SemesterId, s.DayOfWeek, s.SlotNumber }).IsUnique();
        });

        modelBuilder.Entity<Notification>(e =>
        {
            e.HasOne(n => n.User).WithMany().HasForeignKey(n => n.UserId).OnDelete(DeleteBehavior.Cascade);
            e.Property(n => n.Message).HasMaxLength(500).IsRequired();
            e.Property(n => n.Link).HasMaxLength(200);
            e.HasIndex(n => new { n.UserId, n.IsRead });
        });

        modelBuilder.Entity<Excuse>(e =>
        {
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<SystemSettings>(e =>
        {
            e.Property(s => s.DepartmentName).HasMaxLength(50).IsRequired();
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Id == Guid.Empty) entry.Entity.Id = Guid.NewGuid();
                if (entry.Entity.CreatedAt == default) entry.Entity.CreatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
