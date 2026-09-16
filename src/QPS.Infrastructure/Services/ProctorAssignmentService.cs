using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Data;
using QPS.Domain.Entities;
using QPS.Domain.Enums;

namespace QPS.Infrastructure.Services;

public class ProctorAssignmentService : IProctorAssignmentService
{
    private readonly AppDbContext _context;

    public ProctorAssignmentService(AppDbContext context) => _context = context;

    public async Task<List<ProctorAssignment>> AutoAssignAsync(
        Guid quizId, CancellationToken ct = default)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Locations)
            .FirstOrDefaultAsync(q => q.Id == quizId, ct);

        if (quiz == null) return new List<ProctorAssignment>();

        var semesterId = quiz.SemesterId;
        var quizDay = ConvertToDayOfWeek(quiz.QuizDate);
        var assignments = new List<ProctorAssignment>();

        var tas = await _context.Users
            .Where(u => u.Role == UserRole.TA && u.IsActive)
            .ToListAsync(ct);

        var existingAssignments = await _context.ProctorAssignments
            .Where(pa => pa.QuizLocation.QuizId == quizId && !pa.IsDeleted)
            .ToListAsync(ct);

        foreach (var location in quiz.Locations)
        {
            var assignedCount = existingAssignments
                .Count(a => a.QuizLocationId == location.Id && !a.IsBackup);

            var needed = location.ProctorsNeeded - assignedCount;
            if (needed <= 0) continue;

            var eligibleTas = new List<User>();

            foreach (var ta in tas)
            {
                if (ta.DayOff.HasValue && ta.DayOff.Value == quizDay)
                    continue;

                var hasConflict = await _context.TAScheduleSlots
                    .AnyAsync(s =>
                        s.UserId == ta.Id &&
                        s.SemesterId == semesterId &&
                        s.DayOfWeek == quizDay &&
                        s.StartTime < quiz.EndTime &&
                        s.EndTime > quiz.StartTime, ct);

                if (hasConflict) continue;

                var semesterAssignmentCount = await _context.ProctorAssignments
                    .CountAsync(a =>
                        a.UserId == ta.Id &&
                        a.QuizLocation.Quiz.SemesterId == semesterId &&
                        a.Status != AssignmentStatus.Cancelled &&
                        !a.IsDeleted, ct);

                if (semesterAssignmentCount >= ta.MaxProctoringSessionsPerSemester)
                    continue;

                var weekStart = quiz.QuizDate.AddDays(-(int)quiz.QuizDate.DayOfWeek);
                var weekEnd = weekStart.AddDays(7);
                var weekAssignmentCount = await _context.ProctorAssignments
                    .CountAsync(a =>
                        a.UserId == ta.Id &&
                        a.QuizLocation.Quiz.QuizDate >= weekStart &&
                        a.QuizLocation.Quiz.QuizDate < weekEnd &&
                        a.Status != AssignmentStatus.Cancelled &&
                        !a.IsDeleted, ct);

                if (weekAssignmentCount >= 2)
                    continue;

                var alreadyAssigned = existingAssignments
                    .Any(a => a.UserId == ta.Id && a.QuizLocationId == location.Id);

                if (alreadyAssigned) continue;

                eligibleTas.Add(ta);
            }

            var taAssignmentCounts = new Dictionary<Guid, int>();
            foreach (var ta in eligibleTas)
            {
                var count = await _context.ProctorAssignments
                    .CountAsync(a =>
                        a.UserId == ta.Id &&
                        a.Status != AssignmentStatus.Cancelled &&
                        !a.IsDeleted, ct);
                taAssignmentCounts[ta.Id] = count;
            }

            eligibleTas = eligibleTas
                .OrderBy(ta => taAssignmentCounts[ta.Id])
                .ToList();

            var toAssign = eligibleTas.Take(needed).ToList();

            foreach (var ta in toAssign)
            {
                var assignment = new ProctorAssignment
                {
                    QuizLocationId = location.Id,
                    UserId = ta.Id,
                    IsBackup = false,
                    Status = AssignmentStatus.Assigned
                };
                assignments.Add(assignment);
            }

            if (quiz.AddBackup && eligibleTas.Count > needed)
            {
                var backup = eligibleTas.Skip(needed).FirstOrDefault();
                if (backup != null)
                {
                    assignments.Add(new ProctorAssignment
                    {
                        QuizLocationId = location.Id,
                        UserId = backup.Id,
                        IsBackup = true,
                        Status = AssignmentStatus.Assigned
                    });
                }
            }
        }

        _context.ProctorAssignments.AddRange(assignments);
        await _context.SaveChangesAsync(ct);

        return assignments;
    }

    private QPS.Domain.Enums.DayOfWeek ConvertToDayOfWeek(DateOnly date)
    {
        return date.DayOfWeek switch
        {
            System.DayOfWeek.Friday => QPS.Domain.Enums.DayOfWeek.Friday,
            System.DayOfWeek.Saturday => QPS.Domain.Enums.DayOfWeek.Saturday,
            System.DayOfWeek.Sunday => QPS.Domain.Enums.DayOfWeek.Sunday,
            System.DayOfWeek.Monday => QPS.Domain.Enums.DayOfWeek.Monday,
            System.DayOfWeek.Tuesday => QPS.Domain.Enums.DayOfWeek.Tuesday,
            System.DayOfWeek.Wednesday => QPS.Domain.Enums.DayOfWeek.Wednesday,
            System.DayOfWeek.Thursday => QPS.Domain.Enums.DayOfWeek.Thursday,
            _ => QPS.Domain.Enums.DayOfWeek.Friday
        };
    }
}