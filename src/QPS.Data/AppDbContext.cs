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
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<QuizLocation> QuizLocations => Set<QuizLocation>();
    public DbSet<ProctorAssignment> ProctorAssignments => Set<ProctorAssignment>();
    public DbSet<TAScheduleSlot> TAScheduleSlots => Set<TAScheduleSlot>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Excuse> Excuses => Set<Excuse>();
    public DbSet<WorkloadConfig> WorkloadConfigs => Set<WorkloadConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasQueryFilter(u => !u.IsDeleted);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
            entity.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.LastName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
        });

        // Course
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasQueryFilter(c => !c.IsDeleted);
            entity.Property(c => c.Name).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Department).HasMaxLength(50).IsRequired();
        });

        // Semester
        modelBuilder.Entity<Semester>(entity =>
        {
            entity.HasQueryFilter(s => !s.IsDeleted);
            entity.Property(s => s.Name).HasMaxLength(100).IsRequired();
        });

        // Quiz
        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasQueryFilter(q => !q.IsDeleted);
            entity.HasOne(q => q.Course)
                  .WithMany()
                  .HasForeignKey(q => q.CourseId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(q => q.Semester)
                  .WithMany()
                  .HasForeignKey(q => q.SemesterId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // QuizLocation
        modelBuilder.Entity<QuizLocation>(entity =>
        {
            entity.HasQueryFilter(ql => !ql.IsDeleted);
            entity.HasOne(ql => ql.Quiz)
                  .WithMany(q => q.Locations)
                  .HasForeignKey(ql => ql.QuizId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(ql => ql.RoomName).HasMaxLength(50).IsRequired();
        });

        // ProctorAssignment
        modelBuilder.Entity<ProctorAssignment>(entity =>
        {
            entity.HasQueryFilter(pa => !pa.IsDeleted);
            entity.HasOne(pa => pa.QuizLocation)
                  .WithMany(ql => ql.Assignments)
                  .HasForeignKey(pa => pa.QuizLocationId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(pa => pa.User)
                  .WithMany()
                  .HasForeignKey(pa => pa.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // TAScheduleSlot
        modelBuilder.Entity<TAScheduleSlot>(entity =>
        {
            entity.HasQueryFilter(ts => !ts.IsDeleted);
            entity.HasOne(ts => ts.User)
                  .WithMany()
                  .HasForeignKey(ts => ts.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(ts => ts.Semester)
                  .WithMany()
                  .HasForeignKey(ts => ts.SemesterId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.Property(ts => ts.CourseName).HasMaxLength(200).IsRequired();
            entity.Property(ts => ts.Room).HasMaxLength(50).IsRequired();
        });

        // Notification
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasQueryFilter(n => !n.IsDeleted);
            entity.HasOne(n => n.User)
                  .WithMany()
                  .HasForeignKey(n => n.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(n => n.Message).HasMaxLength(500).IsRequired();
        });

        // Excuse
        modelBuilder.Entity<Excuse>(entity =>
        {
            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.Reason).HasMaxLength(500).IsRequired();
        });

        // WorkloadConfig
        modelBuilder.Entity<WorkloadConfig>(entity =>
        {
            entity.HasQueryFilter(w => !w.IsDeleted);
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
