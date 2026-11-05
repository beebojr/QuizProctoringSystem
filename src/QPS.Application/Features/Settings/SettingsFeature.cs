using FluentValidation;
using MediatR;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Common.Services;
using QPS.Domain.Entities;

namespace QPS.Application.Features.Settings;

public record SettingsDto(
    int DefaultMaxSessionsPerSemester,
    int MaxSessionsPerWeek,
    double DefaultTargetWorkload,
    int DefaultProctorsPerRoom,
    int ReserveProctors,
    bool PreferCourseTas,
    string DepartmentName);

public record GetSettingsQuery : IRequest<SettingsDto>;

public record UpdateSettingsCommand(
    int DefaultMaxSessionsPerSemester,
    int MaxSessionsPerWeek,
    double DefaultTargetWorkload,
    int DefaultProctorsPerRoom,
    int ReserveProctors,
    bool PreferCourseTas,
    string DepartmentName) : IRequest<Result<SettingsDto>>;

public class UpdateSettingsValidator : AbstractValidator<UpdateSettingsCommand>
{
    public UpdateSettingsValidator()
    {
        RuleFor(x => x.DefaultMaxSessionsPerSemester).InclusiveBetween(1, 200);
        RuleFor(x => x.MaxSessionsPerWeek).InclusiveBetween(1, 20);
        RuleFor(x => x.DefaultTargetWorkload).InclusiveBetween(0, 200);
        RuleFor(x => x.DefaultProctorsPerRoom).InclusiveBetween(1, 10);
        RuleFor(x => x.ReserveProctors).InclusiveBetween(1, 10);
        RuleFor(x => x.DepartmentName).NotEmpty().MaximumLength(50);
    }
}

public class SettingsHandlers :
    IRequestHandler<GetSettingsQuery, SettingsDto>,
    IRequestHandler<UpdateSettingsCommand, Result<SettingsDto>>
{
    private readonly IApplicationDbContext _db;

    public SettingsHandlers(IApplicationDbContext db) => _db = db;

    public async Task<SettingsDto> Handle(GetSettingsQuery request, CancellationToken ct) =>
        ToDto(await _db.GetSettingsAsync(ct));

    public async Task<Result<SettingsDto>> Handle(UpdateSettingsCommand request, CancellationToken ct)
    {
        var s = await _db.GetSettingsAsync(ct);
        s.DefaultMaxSessionsPerSemester = request.DefaultMaxSessionsPerSemester;
        s.MaxSessionsPerWeek = request.MaxSessionsPerWeek;
        s.DefaultTargetWorkload = request.DefaultTargetWorkload;
        s.DefaultProctorsPerRoom = request.DefaultProctorsPerRoom;
        s.ReserveProctors = request.ReserveProctors;
        s.PreferCourseTas = request.PreferCourseTas;
        s.DepartmentName = request.DepartmentName.Trim();
        await _db.SaveChangesAsync(ct);
        return Result<SettingsDto>.Ok(ToDto(s), "Settings saved.");
    }

    public static SettingsDto ToDto(SystemSettings s) => new(
        s.DefaultMaxSessionsPerSemester, s.MaxSessionsPerWeek, s.DefaultTargetWorkload,
        s.DefaultProctorsPerRoom, s.ReserveProctors, s.PreferCourseTas, s.DepartmentName);
}
