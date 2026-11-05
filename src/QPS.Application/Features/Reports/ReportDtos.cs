using QPS.Application.Features.Assignments.DTOs;
using QPS.Application.Features.Excuses;
using QPS.Domain.Enums;

namespace QPS.Application.Features.Reports;

public record QuizBriefDto(
    Guid Id,
    string CourseName,
    ExamType ExamType,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    int WeekNumber,
    string SlotLabel,
    List<string> Rooms,
    int Needed,
    int Assigned,
    QuizStatus Status);

public record TaRankDto(Guid UserId, string Name, int Assignments, double Points);

public record PersonDto(Guid UserId, string Name);

public record TaSummaryDto(
    Guid UserId,
    string Name,
    string Email,
    bool IsActive,
    List<string> DaysOff,
    int SlotCount,
    bool CanEditSchedule,
    int Assignments,
    int Upcoming,
    int Completed,
    int ThisWeek,
    double Points,
    double TargetWorkload,
    int MaxSessions,
    double? TargetWorkloadOverride,
    int? MaxSessionsOverride,
    int ActiveExcuses,
    bool TargetReached,
    List<string> CoursesTaught);

public record ProctorDetailDto(TaSummaryDto Summary, List<AssignmentDto> History, List<ExcuseDto> Excuses);

public record CourseSummaryDto(
    Guid CourseId,
    string Name,
    string? Code,
    int QuizCount,
    int Past,
    int Upcoming,
    int TotalAssignments,
    int DistinctProctors,
    List<PersonDto> CourseTas,
    List<QuizBriefDto> Quizzes);

public record WeekLoadDto(int Week, DateOnly WeekStart, int Quizzes, int SeatsNeeded, int SeatsFilled);

public record AnalyticsDto(
    int TotalQuizzes,
    int TotalAssignments,
    int Courses,
    int Tas,
    int PastQuizzes,
    int UpcomingQuizzes,
    double AverageAssignments,
    int ZeroAssignmentTas,
    int AboveAverage,
    int BelowAverage,
    double FairnessSpread,
    List<TaRankDto> Distribution,
    List<WeekLoadDto> Weeks);

public record DayLoadDto(DateOnly Date, int Quizzes, int Needed, int Assigned);

public record AdminDashboardDto(
    string Department,
    Guid SemesterId,
    string SemesterName,
    int CurrentWeek,
    int TotalWeeks,
    int ActiveTas,
    int UpcomingQuizzes,
    int QuizzesMissingProctors,
    int EmptySeats,
    int FlaggedAssignments,
    int ActiveExcuses,
    int TasWithoutSchedule,
    List<QuizBriefDto> UpcomingSchedule,
    List<TaRankDto> MostActive,
    List<DayLoadDto> ThisWeek);

public record TaDashboardDto(
    string Name,
    string SemesterName,
    int CurrentWeek,
    double Points,
    double TargetWorkload,
    int MaxSessions,
    int SessionsThisSemester,
    int SessionsThisWeek,
    int MaxPerWeek,
    List<AssignmentDto> Upcoming,
    List<ExcuseDto> ActiveExcuses,
    List<string> DaysOff,
    bool CanEditSchedule,
    int SlotCount);
