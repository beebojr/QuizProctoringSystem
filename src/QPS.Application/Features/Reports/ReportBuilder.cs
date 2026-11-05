using QPS.Application.Common.Services;
using QPS.Domain.Entities;
using QPS.Domain.Enums;
using QPS.Domain.Scheduling;

namespace QPS.Application.Features.Reports;

/// <summary>Pure calculations over a <see cref="SemesterSnapshot"/>.</summary>
public static class ReportBuilder
{
    public static QuizBriefDto Brief(SemesterSnapshot s, Quiz q) => new(
        q.Id, q.Course.Name, q.ExamType, q.QuizDate, q.StartTime, q.EndTime, q.WeekNumber,
        SlotCatalog.Describe(q.SlotNumber),
        q.Locations.Where(l => !l.IsReserve).OrderBy(l => l.SortOrder).Select(l => l.RoomName).ToList(),
        q.Locations.Sum(l => l.ProctorsNeeded),
        q.Locations.Sum(l => Math.Min(l.Assignments.Count, l.ProctorsNeeded)),
        s.StatusOf(q));

    public static List<TaSummaryDto> TaSummaries(SemesterSnapshot s)
    {
        var byUser = s.Assignments.ToLookup(x => x.Assignment.UserId);
        var slots = s.Slots.ToLookup(x => x.UserId);
        var excuses = s.Excuses.Where(e => e.EndDate >= s.Today).ToLookup(e => e.UserId);
        var week = s.CurrentWeek;

        return s.Tas.Select(u =>
        {
            var mine = byUser[u.Id].ToList();
            var points = mine.Sum(x => x.Assignment.Points);
            var target = s.TargetFor(u);
            return new TaSummaryDto(
                u.Id, u.FullName, u.Email, u.IsActive, Format.Days(u.DaysOff), slots[u.Id].Count(), u.CanEditSchedule,
                mine.Count,
                mine.Count(x => s.StatusOf(x.Quiz) == QuizStatus.Upcoming),
                mine.Count(x => s.StatusOf(x.Quiz) == QuizStatus.Finished),
                mine.Count(x => AcademicCalendar.WeekNumber(s.Semester.StartDate, x.Quiz.QuizDate) == week),
                points, target, s.MaxFor(u), u.TargetWorkloadOverride, u.MaxSessionsOverride,
                excuses[u.Id].Count(), target > 0 && points >= target,
                slots[u.Id].Select(x => x.CourseName).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList());
        })
        .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();
    }

    public static List<CourseSummaryDto> CourseSummaries(SemesterSnapshot s, IReadOnlyList<Course> courses)
    {
        var tasById = s.Tas.ToDictionary(t => t.Id);
        var courseTas = s.Slots.Where(x => x.CourseId.HasValue)
            .GroupBy(x => x.CourseId!.Value)
            .ToDictionary(g => g.Key, g => g.Select(x => x.UserId).Distinct()
                .Where(tasById.ContainsKey)
                .Select(id => new PersonDto(id, tasById[id].FullName))
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList());
        var quizzesByCourse = s.Quizzes.ToLookup(q => q.CourseId);

        return courses.Select(c =>
        {
            var quizzes = quizzesByCourse[c.Id].OrderBy(q => q.QuizDate).ThenBy(q => q.StartTime).ToList();
            var assignments = quizzes.SelectMany(q => q.Locations.SelectMany(l => l.Assignments)).ToList();
            return new CourseSummaryDto(
                c.Id, c.Name, c.Code, quizzes.Count,
                quizzes.Count(q => s.StatusOf(q) == QuizStatus.Finished),
                quizzes.Count(q => s.StatusOf(q) == QuizStatus.Upcoming),
                assignments.Count,
                assignments.Select(a => a.UserId).Distinct().Count(),
                courseTas.GetValueOrDefault(c.Id) ?? new List<PersonDto>(),
                quizzes.Select(q => Brief(s, q)).ToList());
        })
        .Where(c => c.QuizCount > 0 || c.CourseTas.Count > 0)
        .OrderByDescending(c => c.QuizCount).ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();
    }

    public static AnalyticsDto Analytics(SemesterSnapshot s)
    {
        var summaries = TaSummaries(s).Where(t => t.IsActive).ToList();
        var working = summaries.Where(t => t.Assignments > 0).ToList();
        var average = working.Count == 0 ? 0 : working.Average(t => (double)t.Assignments);
        var spread = working.Count == 0 ? 0 : working.Max(t => t.Assignments) - working.Min(t => t.Assignments);

        var weeks = s.Quizzes
            .GroupBy(q => AcademicCalendar.WeekNumber(s.Semester.StartDate, q.QuizDate))
            .OrderBy(g => g.Key)
            .Select(g => new WeekLoadDto(g.Key, AcademicCalendar.WeekStart(s.Semester.StartDate, g.Key), g.Count(),
                g.Sum(q => q.Locations.Sum(l => l.ProctorsNeeded)),
                g.Sum(q => q.Locations.Sum(l => Math.Min(l.Assignments.Count, l.ProctorsNeeded)))))
            .ToList();

        return new AnalyticsDto(
            s.Quizzes.Count,
            s.Assignments.Count(),
            s.Quizzes.Select(q => q.CourseId).Distinct().Count(),
            summaries.Count,
            s.Quizzes.Count(q => s.StatusOf(q) == QuizStatus.Finished),
            s.Quizzes.Count(q => s.StatusOf(q) == QuizStatus.Upcoming),
            Math.Round(average, 2),
            summaries.Count - working.Count,
            working.Count(t => t.Assignments > average),
            working.Count(t => t.Assignments < average),
            spread,
            working.OrderBy(t => t.Assignments).ThenBy(t => t.Name)
                .Select(t => new TaRankDto(t.UserId, t.Name, t.Assignments, t.Points)).ToList(),
            weeks);
    }
}
