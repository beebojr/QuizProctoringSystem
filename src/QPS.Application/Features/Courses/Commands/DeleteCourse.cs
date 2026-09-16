using MediatR;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;

namespace QPS.Application.Features.Courses.Commands;

public record DeleteCourseCommand(Guid Id) : IRequest<Result>;

public class DeleteCourseHandler : IRequestHandler<DeleteCourseCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteCourseHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteCourseCommand request, CancellationToken cancellationToken)
    {
        var course = _context.Courses.FirstOrDefault(c => c.Id == request.Id);
        if (course == null)
            return Result.Fail("Course not found");

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Ok("Course deleted successfully");
    }
}