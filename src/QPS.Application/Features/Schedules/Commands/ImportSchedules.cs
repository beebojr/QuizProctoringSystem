using MediatR;
using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Interfaces;
using QPS.Application.Common.Models;
using QPS.Application.Common.Services;
using QPS.Application.Features.Schedules.DTOs;
using QPS.Domain.Entities;
using QPS.Domain.Enums;
using QPS.Domain.Scheduling;

namespace QPS.Application.Features.Schedules.Commands;

/// <summary>Admin bulk import of TA weekly schedules from Excel (Email, Day, Slot, Course, Type, Room).</summary>
public record ImportSchedulesCommand(Stream Content, Guid? SemesterId, bool ReplaceExisting) : IRequest<Result<ScheduleImportResultDto>>;

public class ImportSchedulesHandler : IRequestHandler<ImportSchedulesCommand, Result<ScheduleImportResultDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly IExcelParserService _excel;

    public ImportSchedulesHandler(IApplicationDbContext db, IExcelParserService excel)
    {
        _db = db;
        _excel = excel;
    }

    public async Task<Result<ScheduleImportResultDto>> Handle(ImportSchedulesCommand request, CancellationToken ct)
    {
        IReadOnlyList<RawScheduleRow> rows;
        try
        {
            rows = _excel.ReadSchedule(request.Content);
        }
        catch (Exception ex) when (ex is InvalidDataException or InvalidOperationException or ArgumentException)
        {
            return Result<ScheduleImportResultDto>.Fail($"That file couldn't be read as an Excel workbook ({ex.Message}).");
        }

        var semester = await _db.ResolveSemesterAsync(request.SemesterId, ct);
        var tas = await _db.Users.Where(u => u.Role == UserRole.TA && !u.IsDeleted).ToListAsync(ct);
        var byEmail = tas.ToDictionary(u => u.Email, StringComparer.OrdinalIgnoreCase);
        var courses = (await _db.Courses.ToListAsync(ct)).GroupBy(c => c.NormalizedName).ToDictionary(g => g.Key, g => g.First());

        var errors = new List<ScheduleImportErrorDto>();
        var parsed = new List<TAScheduleSlot>();
        var seen = new HashSet<(Guid, QPS.Domain.Enums.DayOfWeek, int)>();

        foreach (var row in rows)
        {
            if (string.IsNullOrWhiteSpace(row.Email))
            {
                errors.Add(new(row.RowNumber, "Missing TA email."));
                continue;
            }
            if (!byEmail.TryGetValue(row.Email.Trim(), out var ta))
            {
                errors.Add(new(row.RowNumber, $"No TA with email {row.Email.Trim()}."));
                continue;
            }
            if (!DayParser.TryParse(row.Day, out var day) || day == QPS.Domain.Enums.DayOfWeek.Friday)
            {
                errors.Add(new(row.RowNumber, $"\"{row.Day}\" is not a working day (Saturday–Thursday)."));
                continue;
            }
            if (ta.DaysOff.HasFlag(AcademicCalendar.ToFlag(day)))
            {
                errors.Add(new(row.RowNumber, $"{day} is {ta.FullName}'s day off."));
                continue;
            }
            if (!SlotCatalog.TryParse(row.Slot, out var slotDef) || !SlotCatalog.IsTeachingSlot(slotDef.Number))
            {
                errors.Add(new(row.RowNumber, $"\"{row.Slot}\" is not a teaching slot (1–5)."));
                continue;
            }
            var courseName = NameKey.Clean(row.Course);
            if (courseName.Length == 0)
            {
                errors.Add(new(row.RowNumber, "Missing course."));
                continue;
            }
            if (!seen.Add((ta.Id, day, slotDef.Number)))
            {
                errors.Add(new(row.RowNumber, $"{ta.FullName} has Slot {slotDef.Number} on {day} twice in the file."));
                continue;
            }

            courses.TryGetValue(NameKey.For(courseName), out var course);
            parsed.Add(new TAScheduleSlot
            {
                UserId = ta.Id,
                SemesterId = semester.Id,
                DayOfWeek = day,
                SlotNumber = slotDef.Number,
                CourseId = course?.Id,
                CourseName = course?.Name ?? courseName,
                SlotType = ParseType(row.Type),
                Room = NameKey.Clean(row.Room).Length > 0 ? NameKey.Clean(row.Room) : "TBA",
                StartTime = slotDef.Start,
                EndTime = slotDef.End
            });
        }

        var replacedTas = 0;
        var tasInFile = parsed.Select(p => p.UserId).Distinct().ToList();
        var existing = await _db.TAScheduleSlots.Where(s => s.SemesterId == semester.Id && tasInFile.Contains(s.UserId)).ToListAsync(ct);

        if (request.ReplaceExisting)
        {
            _db.TAScheduleSlots.RemoveRange(existing);
            replacedTas = existing.Select(e => e.UserId).Distinct().Count();
        }
        else
        {
            var taken = existing.Select(e => (e.UserId, e.DayOfWeek, e.SlotNumber)).ToHashSet();
            var clashes = parsed.Where(p => taken.Contains((p.UserId, p.DayOfWeek, p.SlotNumber))).ToList();
            foreach (var c in clashes)
            {
                var name = tas.First(t => t.Id == c.UserId).FullName;
                errors.Add(new(0, $"{name} already has Slot {c.SlotNumber} on {c.DayOfWeek}; kept the existing slot."));
                parsed.Remove(c);
            }
        }

        _db.TAScheduleSlots.AddRange(parsed);
        await _db.SaveChangesAsync(ct);

        return Result<ScheduleImportResultDto>.Ok(
            new ScheduleImportResultDto(parsed.Count, replacedTas, errors),
            $"Imported {parsed.Count} slot{(parsed.Count == 1 ? "" : "s")}.");
    }

    private static SlotType ParseType(string? text)
    {
        var t = (text ?? string.Empty).Trim().ToLowerInvariant();
        if (t.StartsWith("tut")) return SlotType.Tut;
        if (t.StartsWith("lec")) return SlotType.Lecture;
        return SlotType.Lab;
    }
}
