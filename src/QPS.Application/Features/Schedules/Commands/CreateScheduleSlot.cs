using FluentValidation;
using MediatR;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Features.Schedules.DTOs;
using QPS.Domain.Entities;
using QPS.Domain.Enums;

namespace QPS.Application.Features.Schedules.Commands;

public class CreateScheduleSlotValidator : AbstractValidator<CreateScheduleSlotCommand>
{
    public CreateScheduleSlotValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.SemesterId).NotEmpty();
        RuleFor(x => x.SlotNumber).InclusiveBetween(1, 5);
        RuleFor(x => x.CourseName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Room).NotEmpty().MaximumLength(50);
    }
}

public class CreateScheduleSlotHandler : IRequestHandler<CreateScheduleSlotCommand, Result<ScheduleSlotDto>>
{
    private readonly IApplicationDbContext _context;

    public CreateScheduleSlotHandler(IApplicationDbContext context) => _context = context;

    public async Task<Result<ScheduleSlotDto>> Handle(
        CreateScheduleSlotCommand request, CancellationToken cancellationToken)
    {
        var slotTimes = new Dictionary<int, (TimeOnly Start, TimeOnly End)>
        {
            { 1, (new TimeOnly(8, 30), new TimeOnly(10, 0)) },
            { 2, (new TimeOnly(10, 15), new TimeOnly(11, 45)) },
            { 3, (new TimeOnly(12, 0), new TimeOnly(13, 30)) },
            { 4, (new TimeOnly(13, 45), new TimeOnly(15, 15)) },
            { 5, (new TimeOnly(15, 45), new TimeOnly(17, 15)) }
        };

        var (start, end) = slotTimes[request.SlotNumber];

        var slot = new TAScheduleSlot
        {
            UserId = request.UserId,
            SemesterId = request.SemesterId,
            DayOfWeek = request.DayOfWeek,
            SlotNumber = request.SlotNumber,
            CourseName = request.CourseName.Trim(),
            SlotType = request.SlotType,
            Room = request.Room.Trim(),
            StartTime = start,
            EndTime = end
        };

        _context.TAScheduleSlots.Add(slot);
        await _context.SaveChangesAsync(cancellationToken);

        var user = _context.Users.First(u => u.Id == request.UserId);
        var dto = new ScheduleSlotDto(
            slot.Id, slot.UserId, user.FullName, slot.DayOfWeek, slot.SlotNumber,
            slot.CourseName, slot.SlotType, slot.Room, slot.StartTime, slot.EndTime);

        return Result<ScheduleSlotDto>.Ok(dto, "Schedule slot created");
    }
}
