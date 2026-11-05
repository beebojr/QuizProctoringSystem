using QPS.Application.Common.Services;
using QPS.Application.Features.Quizzes.DTOs;
using QPS.Domain.Entities;
using QPS.Domain.Enums;
using QPS.Domain.Rules;
using QPS.Domain.Scheduling;

namespace QPS.Application.Features.Quizzes;

public static class QuizMapper
{
    /// <summary>
    /// Maps a quiz (Course, Locations, Assignments and their Users loaded). When a context is
    /// given, assignments of upcoming quizzes are re-checked so stale ones (new excuse, new
    /// teaching slot, deactivated TA) are flagged.
    /// </summary>
    public static QuizDto ToDto(Quiz quiz, DateTime localNow, AssignmentContext? context = null)
    {
        var status = QuizTiming.StatusAt(quiz.QuizDate, quiz.StartTime, quiz.EndTime, localNow);
        var session = context?.SessionFor(quiz);
        var check = context is not null && status == QuizStatus.Upcoming;

        var locations = quiz.Locations
            .OrderBy(l => l.IsReserve).ThenBy(l => l.SortOrder).ThenBy(l => l.RoomName, StringComparer.OrdinalIgnoreCase)
            .Select(l => new QuizLocationDto(
                l.Id, l.RoomName, l.Group, l.ProctorsNeeded, l.IsReserve, l.SortOrder, l.Assignments.Count,
                l.Assignments
                    .OrderByDescending(a => a.IsCourseTa).ThenBy(a => a.CreatedAt).ThenBy(a => a.User?.FirstName)
                    .Select(a => new ProctorChipDto(
                        a.Id, a.UserId, a.User?.FullName ?? "Unknown", a.User?.Email ?? string.Empty,
                        a.IsCourseTa, a.Source, a.OverrideReason,
                        check
                            ? context!.Revalidate(a.UserId, session!)
                                .Where(v => v.Code != RuleCode.AlreadyInQuiz)
                                .Select(v => v.Message).ToList()
                            : new List<string>()))
                    .ToList()))
            .ToList();

        var needed = locations.Sum(l => l.ProctorsNeeded);
        var assigned = locations.Sum(l => Math.Min(l.Assigned, l.ProctorsNeeded));
        var warnings = locations.Sum(l => l.Proctors.Count(p => p.Warnings.Count > 0));

        return new QuizDto(
            quiz.Id, quiz.CourseId, quiz.Course?.Name ?? string.Empty, quiz.SemesterId, quiz.ExamType,
            quiz.WeekNumber, quiz.QuizDate, quiz.StartTime, quiz.EndTime, quiz.SlotNumber,
            SlotCatalog.Describe(quiz.SlotNumber), status, quiz.AutoAssign, quiz.HasReserve, quiz.Notes,
            quiz.Source, needed, assigned, assigned >= needed, warnings, locations);
    }

    public static ShortfallDto ToDto(Shortfall s, Quiz quiz) => new(
        s.QuizId, s.LocationId, quiz.Course?.Name ?? string.Empty, quiz.QuizDate, quiz.StartTime, quiz.EndTime,
        s.RoomName, s.Missing,
        s.Reasons.Select(r => $"{r.Count} {r.Label}").ToList());
}
