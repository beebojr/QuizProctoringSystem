using FluentValidation;
using MediatR;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Features.Quizzes.DTOs;
using QPS.Domain.Entities;
using QPS.Domain.Enums;

namespace QPS.Application.Features.Quizzes.Commands;

public class CreateQuizValidator : AbstractValidator<CreateQuizCommand>
{
    public CreateQuizValidator()
    {
        RuleFor(x => x.CourseId).NotEmpty();
        RuleFor(x => x.SemesterId).NotEmpty();
        RuleFor(x => x.QuizDate).NotEmpty();
        RuleFor(x => x.EndTime).GreaterThan(x => x.StartTime);
        RuleFor(x => x.Locations).NotEmpty();
    }
}

public class CreateQuizHandler : IRequestHandler<CreateQuizCommand, Result<QuizDto>>
{
    private readonly IApplicationDbContext _context;

    public CreateQuizHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<QuizDto>> Handle(
        CreateQuizCommand request, CancellationToken cancellationToken)
    {
        var quiz = new Quiz
        {
            CourseId = request.CourseId,
            SemesterId = request.SemesterId,
            QuizDate = request.QuizDate,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Status = QuizStatus.Upcoming,
            AutoAssign = request.AutoAssign,
            AddBackup = request.AddBackup
        };

        foreach (var loc in request.Locations)
        {
            quiz.Locations.Add(new QuizLocation
            {
                RoomName = loc.RoomName,
                ProctorsNeeded = loc.ProctorsNeeded
            });
        }

        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync(cancellationToken);

        var course = _context.Courses.First(c => c.Id == request.CourseId);
        var dto = MapToDto(quiz, course.Name);
        return Result<QuizDto>.Ok(dto, "Quiz created successfully");
    }

    private QuizDto MapToDto(Quiz quiz, string courseName)
    {
        return new QuizDto(
            quiz.Id,
            courseName,
            quiz.QuizDate,
            quiz.StartTime,
            quiz.EndTime,
            quiz.Status,
            quiz.AutoAssign,
            quiz.AddBackup,
            quiz.Locations.Select(l => new QuizLocationDto(
                l.Id,
                l.RoomName,
                l.ProctorsNeeded,
                l.Assignments.Select(a => new ProctorAssignmentDto(
                    a.Id,
                    $"{a.User.FirstName} {a.User.LastName}",
                    a.IsBackup,
                    a.Status)).ToList())).ToList());
    }
}