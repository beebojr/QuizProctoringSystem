using MediatR;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;

namespace QPS.Application.Features.Schedules.Commands;

public record DeleteScheduleSlotCommand(Guid Id) : IRequest<Result>;

public class DeleteScheduleSlotHandler : IRequestHandler<DeleteScheduleSlotCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public DeleteScheduleSlotHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result> Handle(DeleteScheduleSlotCommand request, CancellationToken cancellationToken)
    {
        var slot = _context.TAScheduleSlots.FirstOrDefault(s => s.Id == request.Id);
        if (slot == null)
            return Result.Fail("Schedule slot not found");

        _context.TAScheduleSlots.Remove(slot);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Ok("Schedule slot deleted");
    }
}
