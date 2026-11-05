using Microsoft.EntityFrameworkCore;
using QPS.Domain.Entities;

namespace QPS.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Course> Courses { get; }
    DbSet<Semester> Semesters { get; }
    DbSet<Room> Rooms { get; }
    DbSet<Quiz> Quizzes { get; }
    DbSet<QuizLocation> QuizLocations { get; }
    DbSet<ProctorAssignment> ProctorAssignments { get; }
    DbSet<TAScheduleSlot> TAScheduleSlots { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<Excuse> Excuses { get; }
    DbSet<SystemSettings> SystemSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
