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
                var courseName = row.Cell(1).GetString().Trim();
                var dateStr = row.Cell(2).GetString().Trim();
                var startTimeStr = row.Cell(3).GetString().Trim();
                var endTimeStr = row.Cell(4).GetString().Trim();
                var roomName = row.Cell(5).GetString().Trim();
                var proctorsNeededStr = row.Cell(6).GetString().Trim();

                var course = _context.Courses.FirstOrDefault(c => c.Name == courseName);
                if (course == null)
                {
                    errors.Add($"Row {row.RowNumber()}: Course '{courseName}' not found");
                    continue;
                }

                if (!DateOnly.TryParse(dateStr, out var quizDate))
                {
                    errors.Add($"Row {row.RowNumber()}: Invalid date '{dateStr}'");
                    continue;
                }

                if (!TimeOnly.TryParse(startTimeStr, out var startTime))
                {
                    errors.Add($"Row {row.RowNumber()}: Invalid start time '{startTimeStr}'");
                    continue;
                }

                if (!TimeOnly.TryParse(endTimeStr, out var endTime))
                {
                    errors.Add($"Row {row.RowNumber()}: Invalid end time '{endTimeStr}'");
                    continue;
                }

                var proctorsNeeded = 1;
                if (!string.IsNullOrWhiteSpace(proctorsNeededStr))
                    int.TryParse(proctorsNeededStr, out proctorsNeeded);

                var existingQuiz = _context.Quizzes.FirstOrDefault(q =>
                    q.CourseId == course.Id &&
                    q.QuizDate == quizDate &&
                    q.StartTime == startTime);

                if (existingQuiz == null)
                {
                    var quiz = new Quiz
                    {
                        CourseId = course.Id,
                        SemesterId = request.SemesterId,
                        QuizDate = quizDate,
                        StartTime = startTime,
                        EndTime = endTime,
                        Status = QuizStatus.Upcoming,
                        AutoAssign = false,
                        AddBackup = false
                    };
                    quiz.Locations.Add(new QuizLocation
                    {
                        RoomName = roomName,
                        ProctorsNeeded = proctorsNeeded
                    });
                    _context.Quizzes.Add(quiz);
                }
                else
                {
                    existingQuiz.Locations.Add(new QuizLocation
                    {
                        RoomName = roomName,
                        ProctorsNeeded = proctorsNeeded
                    });
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
}