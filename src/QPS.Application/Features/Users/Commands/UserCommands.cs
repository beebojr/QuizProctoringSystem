using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Exceptions;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Common.Services;
using QPS.Application.Features.Users.DTOs;
using QPS.Domain.Entities;
using QPS.Domain.Enums;
using DayOfWeek = QPS.Domain.Enums.DayOfWeek;

namespace QPS.Application.Features.Users.Commands;

public record CreateUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    UserRole Role,
    string? Department = null,
    List<DayOfWeek>? DaysOff = null,
    int? MaxSessionsOverride = null,
    double? TargetWorkloadOverride = null,
    bool CanEditSchedule = true) : IRequest<Result<UserDto>>;

public record UpdateUserCommand(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    string? Department,
    List<DayOfWeek>? DaysOff,
    int? MaxSessionsOverride,
    double? TargetWorkloadOverride,
    bool CanEditSchedule,
    bool IsActive,
    string? NewPassword = null) : IRequest<Result<UserDto>>;

public record DeleteUserCommand(Guid Id) : IRequest<Result>;

/// <summary>Turns "TA can edit their weekly schedule" on/off for one TA, or every TA when UserId is null.</summary>
public record SetScheduleEditingCommand(Guid? UserId, bool Enabled) : IRequest<Result>;

/// <summary>Per-TA workload goal and Rule 1 limit. Null resets to the global default.</summary>
public record UpdateWorkloadCommand(Guid UserId, double? TargetWorkloadOverride, int? MaxSessionsOverride) : IRequest<Result<UserDto>>;

public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.DaysOff).Must(d => d == null || d.Count(x => x != DayOfWeek.Friday) <= 3)
            .WithMessage("A TA can have at most 3 extra days off besides Friday.");
        RuleFor(x => x.MaxSessionsOverride).InclusiveBetween(0, 200).When(x => x.MaxSessionsOverride.HasValue);
        RuleFor(x => x.TargetWorkloadOverride).InclusiveBetween(0, 200).When(x => x.TargetWorkloadOverride.HasValue);
    }
}

public class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.NewPassword).MinimumLength(8).MaximumLength(100).When(x => !string.IsNullOrEmpty(x.NewPassword));
        RuleFor(x => x.DaysOff).Must(d => d == null || d.Count(x => x != DayOfWeek.Friday) <= 3)
            .WithMessage("A TA can have at most 3 extra days off besides Friday.");
        RuleFor(x => x.MaxSessionsOverride).InclusiveBetween(0, 200).When(x => x.MaxSessionsOverride.HasValue);
        RuleFor(x => x.TargetWorkloadOverride).InclusiveBetween(0, 200).When(x => x.TargetWorkloadOverride.HasValue);
    }
}

public class UpdateWorkloadValidator : AbstractValidator<UpdateWorkloadCommand>
{
    public UpdateWorkloadValidator()
    {
        RuleFor(x => x.MaxSessionsOverride).InclusiveBetween(0, 200).When(x => x.MaxSessionsOverride.HasValue);
        RuleFor(x => x.TargetWorkloadOverride).InclusiveBetween(0, 200).When(x => x.TargetWorkloadOverride.HasValue);
    }
}

public class UserCommandHandlers :
    IRequestHandler<CreateUserCommand, Result<UserDto>>,
    IRequestHandler<UpdateUserCommand, Result<UserDto>>,
    IRequestHandler<DeleteUserCommand, Result>,
    IRequestHandler<SetScheduleEditingCommand, Result>,
    IRequestHandler<UpdateWorkloadCommand, Result<UserDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IClock _clock;
    private readonly INotificationService _notifications;

    public UserCommandHandlers(IApplicationDbContext db, ICurrentUserService currentUser, IClock clock, INotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
        _notifications = notifications;
    }

    public async Task<Result<UserDto>> Handle(CreateUserCommand request, CancellationToken ct)
    {
        var settings = await _db.GetSettingsAsync(ct);
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            return Result<UserDto>.Fail($"{email} is already registered.");

        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Role = request.Role,
            Department = string.IsNullOrWhiteSpace(request.Department) ? settings.DepartmentName : request.Department.Trim(),
            DaysOff = DaysOffInput.ToFlags(request.DaysOff),
            MaxSessionsOverride = request.MaxSessionsOverride,
            TargetWorkloadOverride = request.TargetWorkloadOverride,
            CanEditSchedule = request.CanEditSchedule,
            IsActive = true
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
        return Result<UserDto>.Ok(UserDto.From(user, settings), $"{user.FullName} added.");
    }

    public async Task<Result<UserDto>> Handle(UpdateUserCommand request, CancellationToken ct)
    {
        var settings = await _db.GetSettingsAsync(ct);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.Id && !u.IsDeleted, ct)
                   ?? throw new NotFoundException("User", request.Id);
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != user.Id, ct))
            return Result<UserDto>.Fail($"{email} is already registered.");

        var isSelf = _currentUser.UserId == user.Id;
        if (isSelf && request.Role != UserRole.Admin)
            return Result<UserDto>.Fail("You can't remove your own admin role.");
        if (isSelf && !request.IsActive)
            return Result<UserDto>.Fail("You can't deactivate your own account.");

        user.Email = email;
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Role = request.Role;
        user.Department = string.IsNullOrWhiteSpace(request.Department) ? user.Department : request.Department.Trim();
        user.DaysOff = DaysOffInput.ToFlags(request.DaysOff);
        user.MaxSessionsOverride = request.MaxSessionsOverride;
        user.TargetWorkloadOverride = request.TargetWorkloadOverride;
        user.CanEditSchedule = request.CanEditSchedule;
        user.IsActive = request.IsActive;
        if (!string.IsNullOrEmpty(request.NewPassword))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        await _db.SaveChangesAsync(ct);
        return Result<UserDto>.Ok(UserDto.From(user, settings), "Changes saved.");
    }

    public async Task<Result> Handle(DeleteUserCommand request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.Id && !u.IsDeleted, ct)
                   ?? throw new NotFoundException("User", request.Id);
        if (_currentUser.UserId == user.Id)
            return Result.Fail("You can't delete your own account.");

        var assignments = await _db.ProctorAssignments
            .Include(a => a.QuizLocation).ThenInclude(l => l.Quiz).ThenInclude(q => q.Course)
            .Where(a => a.UserId == user.Id)
            .ToListAsync(ct);

        if (assignments.Count == 0)
        {
            _db.Users.Remove(user);
            await _db.SaveChangesAsync(ct);
            return Result.Ok($"{user.FullName} deleted.");
        }

        // Keep past proctoring history; free the seats of quizzes that haven't happened yet.
        var now = _clock.Now;
        var upcoming = assignments.Where(a => a.QuizLocation.Quiz.QuizDate.ToDateTime(a.QuizLocation.Quiz.StartTime) > now).ToList();
        _db.ProctorAssignments.RemoveRange(upcoming);

        var name = user.FullName;
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.DeletedBy = _currentUser.UserId?.ToString();
        user.IsActive = false;
        user.Email = $"deleted-{user.Id:N}@removed.local";
        await _db.SaveChangesAsync(ct);

        if (upcoming.Count > 0)
            await _notifications.NotifyAdminsAsync(
                $"{name} was removed; {upcoming.Count} upcoming proctoring seat{(upcoming.Count == 1 ? " is" : "s are")} now empty.",
                NotificationType.Assignment, "/quizzes?incomplete=1", ct);
        await _db.SaveChangesAsync(ct);

        return Result.Ok(upcoming.Count == 0
            ? $"{name} deleted. Past proctoring history was kept."
            : $"{name} deleted. {upcoming.Count} upcoming seat(s) were freed.");
    }

    public async Task<Result> Handle(SetScheduleEditingCommand request, CancellationToken ct)
    {
        var query = _db.Users.Where(u => u.Role == UserRole.TA && !u.IsDeleted);
        if (request.UserId.HasValue) query = query.Where(u => u.Id == request.UserId.Value);
        var users = await query.ToListAsync(ct);
        if (request.UserId.HasValue && users.Count == 0) throw new NotFoundException("TA", request.UserId.Value);

        foreach (var u in users.Where(u => u.CanEditSchedule != request.Enabled))
        {
            u.CanEditSchedule = request.Enabled;
            _notifications.Notify(u.Id,
                request.Enabled ? "You can now edit your weekly schedule." : "Schedule editing was turned off by an administrator.",
                NotificationType.Schedule, "/my-schedule");
        }
        await _db.SaveChangesAsync(ct);
        return Result.Ok(request.Enabled ? "Schedule editing turned on." : "Schedule editing turned off.");
    }

    public async Task<Result<UserDto>> Handle(UpdateWorkloadCommand request, CancellationToken ct)
    {
        var settings = await _db.GetSettingsAsync(ct);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId && !u.IsDeleted, ct)
                   ?? throw new NotFoundException("User", request.UserId);
        user.TargetWorkloadOverride = request.TargetWorkloadOverride;
        user.MaxSessionsOverride = request.MaxSessionsOverride;
        await _db.SaveChangesAsync(ct);
        return Result<UserDto>.Ok(UserDto.From(user, settings), "Workload updated.");
    }
}
