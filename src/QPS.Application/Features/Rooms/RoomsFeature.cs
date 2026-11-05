using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Exceptions;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Domain.Entities;
using QPS.Domain.Scheduling;

namespace QPS.Application.Features.Rooms;

public record RoomDto(Guid Id, string Name, int? Capacity, int DefaultProctors, int TimesUsed);

public record GetRoomsQuery : IRequest<List<RoomDto>>;
public record SaveRoomCommand(Guid? Id, string Name, int? Capacity, int DefaultProctors) : IRequest<Result<RoomDto>>;
public record DeleteRoomCommand(Guid Id) : IRequest<Result>;

public class SaveRoomValidator : AbstractValidator<SaveRoomCommand>
{
    public SaveRoomValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DefaultProctors).InclusiveBetween(1, 10);
        RuleFor(x => x.Capacity).InclusiveBetween(1, 2000).When(x => x.Capacity.HasValue);
    }
}

public class RoomHandlers :
    IRequestHandler<GetRoomsQuery, List<RoomDto>>,
    IRequestHandler<SaveRoomCommand, Result<RoomDto>>,
    IRequestHandler<DeleteRoomCommand, Result>
{
    private readonly IApplicationDbContext _db;

    public RoomHandlers(IApplicationDbContext db) => _db = db;

    public async Task<List<RoomDto>> Handle(GetRoomsQuery request, CancellationToken ct)
    {
        var rooms = await _db.Rooms.AsNoTracking().ToListAsync(ct);
        var usage = (await _db.QuizLocations.AsNoTracking().Select(l => l.RoomName).ToListAsync(ct))
            .GroupBy(NameKey.For).ToDictionary(g => g.Key, g => g.Count());
        return rooms
            .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .Select(r => new RoomDto(r.Id, r.Name, r.Capacity, r.DefaultProctors, usage.GetValueOrDefault(r.NormalizedName)))
            .ToList();
    }

    public async Task<Result<RoomDto>> Handle(SaveRoomCommand request, CancellationToken ct)
    {
        var name = NameKey.Clean(request.Name);
        var key = NameKey.For(name);
        if (await _db.Rooms.AnyAsync(r => r.NormalizedName == key && r.Id != request.Id, ct))
            return Result<RoomDto>.Fail($"Room {name} already exists.");

        Room room;
        if (request.Id is { } id && id != Guid.Empty)
        {
            room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == id, ct) ?? throw new NotFoundException("Room", id);
        }
        else
        {
            room = new Room();
            _db.Rooms.Add(room);
        }

        room.Name = name;
        room.NormalizedName = key;
        room.Capacity = request.Capacity;
        room.DefaultProctors = request.DefaultProctors;
        await _db.SaveChangesAsync(ct);
        return Result<RoomDto>.Ok(new RoomDto(room.Id, room.Name, room.Capacity, room.DefaultProctors, 0), "Room saved.");
    }

    public async Task<Result> Handle(DeleteRoomCommand request, CancellationToken ct)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(r => r.Id == request.Id, ct)
                   ?? throw new NotFoundException("Room", request.Id);
        _db.Rooms.Remove(room);
        await _db.SaveChangesAsync(ct);
        return Result.Ok("Room removed. Quizzes already using it keep their location.");
    }
}
