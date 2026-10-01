using ClosedXML.Excel;
using FluentValidation;
using MediatR;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Features.Quizzes.DTOs;
using QPS.Domain.Entities;
using QPS.Domain.Enums;

namespace QPS.Application.Features.Quizzes.Commands;

public class ImportQuizzesValidator : AbstractValidator<ImportQuizzesCommand>
{
    public ImportQuizzesValidator()
    {
        RuleFor(x => x.SemesterId).NotEmpty();
        RuleFor(x => x.File).NotNull();
    }
}

public class ImportQuizzesHandler : IRequestHandler<ImportQuizzesCommand, Result<List<QuizDto>>>
{
    private readonly IApplicationDbContext _context;

    private static readonly Dictionary<string, (TimeOnly Start, TimeOnly End, int SlotNumber)> SlotMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "1ST", (new TimeOnly(8, 30), new TimeOnly(10, 0), 1) },
        { "2ND", (new TimeOnly(10, 15), new TimeOnly(11, 45), 2) },
        { "3RD", (new TimeOnly(12, 0), new TimeOnly(13, 30), 3) },
        { "4TH", (new TimeOnly(13, 45), new TimeOnly(15, 15), 4) },
        { "GAP", (new TimeOnly(15, 15), new TimeOnly(15, 45), 0) },
        { "5TH", (new TimeOnly(15, 45), new TimeOnly(17, 15), 5) },
    };

    public ImportQuizzesHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<List<QuizDto>>> Handle(
        ImportQuizzesCommand request, CancellationToken cancellationToken)
    {
        var quizzes = new List<QuizDto>();
        var errors = new List<string>();

        using var stream = request.File.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();
        var rows = worksheet.RangeUsed().RowsUsed().Skip(1);

        foreach (var row in rows)
        {
            try
            {
                // Column 1: Week (e.g., "Week 7")
                // Column 2: Date (e.g., "Tuesday, April 21, 2026")
                // Column 3: Subject (course name)
                // Column 4: Slot (1ST, 2ND, 3RD, 4TH, 5TH, Gap)
                // Column 5: GP (GPI, GPII, GPIII)
                // Column 6: Location (hyphen-separated rooms)

                var weekStr = row.Cell(1).GetString().Trim();
                var dateStr = row.Cell(2).GetString().Trim();
                var courseName = row.Cell(3).GetString().Trim();
                var slotStr = row.Cell(4).GetString().Trim();
                var group = row.Cell(5).GetString().Trim();
                var locationStr = row.Cell(6).GetString().Trim();

                // Parse week number (e.g., "Week 7" → 7)
                int weekNumber = 0;
                if (!string.IsNullOrWhiteSpace(weekStr))
                {
                    var weekDigits = new string(weekStr.Where(char.IsDigit).ToArray());
                    int.TryParse(weekDigits, out weekNumber);
                }

                // Skip empty rows
                if (string.IsNullOrWhiteSpace(courseName) && string.IsNullOrWhiteSpace(locationStr))
                    continue;

                // Skip rows without a course or location
                if (string.IsNullOrWhiteSpace(courseName) || string.IsNullOrWhiteSpace(locationStr))
                {
                    errors.Add($"Row {row.RowNumber()}: Missing course name or location");
                    continue;
                }

                // Find course
                var course = _context.Courses.FirstOrDefault(c => c.Name == courseName);
                if (course == null)
                {
                    errors.Add($"Row {row.RowNumber()}: Course '{courseName}' not found");
                    continue;
                }

                // Parse date (format: "Tuesday, April 21, 2026")
                if (!TryParseFormattedDate(dateStr, out var quizDate))
                {
                    errors.Add($"Row {row.RowNumber()}: Invalid date '{dateStr}'");
                    continue;
                }

                // Parse slot
                if (!SlotMap.TryGetValue(slotStr, out var slotInfo))
                {
                    errors.Add($"Row {row.RowNumber()}: Invalid slot '{slotStr}'");
                    continue;
                }

                // Split locations (e.g., "M1.105-M1.205" → ["M1.105", "M1.205"])
                var rooms = locationStr.Split('-', StringSplitOptions.RemoveEmptyEntries)
                    .Select(r => r.Trim())
                    .ToList();

                // Find or create quiz for this date + course + slot + group
                var existingQuiz = _context.Quizzes.FirstOrDefault(q =>
                    q.CourseId == course.Id &&
                    q.QuizDate == quizDate &&
                    q.StartTime == slotInfo.Start &&
                    q.Group == group);

                if (existingQuiz == null)
                {
                    var quiz = new Quiz
                    {
                        CourseId = course.Id,
                        SemesterId = request.SemesterId,
                        QuizDate = quizDate,
                        StartTime = slotInfo.Start,
                        EndTime = slotInfo.End,
                        WeekNumber = weekNumber,
                        SlotNumber = slotInfo.SlotNumber,
                        Group = group,
                        Status = QuizStatus.Upcoming,
                        AutoAssign = false,
                        AddBackup = false
                    };

                    foreach (var room in rooms)
                    {
                        quiz.Locations.Add(new QuizLocation
                        {
                            RoomName = room,
                            ProctorsNeeded = 1
                        });
                    }

                    _context.Quizzes.Add(quiz);
                }
                else
                {
                    // Add new locations to existing quiz
                    foreach (var room in rooms)
                    {
                        var alreadyExists = existingQuiz.Locations.Any(l => l.RoomName == room);
                        if (!alreadyExists)
                        {
                            existingQuiz.Locations.Add(new QuizLocation
                            {
                                RoomName = room,
                                ProctorsNeeded = 1
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Row {row.RowNumber()}: {ex.Message}");
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        if (errors.Any())
            return Result<List<QuizDto>>.Fail(errors);

        return Result<List<QuizDto>>.Ok(new List<QuizDto>(), "Import completed successfully");
    }

    private bool TryParseFormattedDate(string dateStr, out DateOnly date)
    {
        date = default;

        // Handle format: "Tuesday, April 21, 2026"
        // Remove the day name part if present
        var commaIndex = dateStr.IndexOf(',');
        if (commaIndex >= 0)
        {
            dateStr = dateStr.Substring(commaIndex + 1).Trim();
        }

        // Try parsing with different formats
        string[] formats = {
            "MMMM d, yyyy",   // "April 21, 2026"
            "MMMM dd, yyyy",  // "April 21, 2026"
            "MMM d, yyyy",    // "Apr 21, 2026"
            "MMM dd, yyyy",   // "Apr 21, 2026"
            "M/d/yyyy",       // "4/21/2026"
            "MM/dd/yyyy"      // "04/21/2026"
        };

        return DateOnly.TryParseExact(dateStr, formats,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out date);
    }
}
