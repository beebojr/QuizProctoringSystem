using QPS.Domain.Enums;

namespace QPS.Application.Features.Dashboard.DTOs;

public record WorkloadSummaryDto(
    int CurrentSemesterSessions,
    int TargetWorkload,
    int RemainingSessions,
    double WorkloadPercentage);

public record SystemDashboardDto(
    int TotalTAs,
    int ActiveTAs,
    int TotalQuizzes,
    int UpcomingQuizzes,
    int AssignedQuizzes,
    int UnassignedQuizzes);

public record ReportDto(
    DateOnly FromDate,
    DateOnly ToDate,
    List<TAReportDto> TAReports);

public record TAReportDto(
    string TAName,
    string TAEmail,
    int TotalSessions,
    int CompletedSessions,
    int UpcomingSessions,
    double Percentage);