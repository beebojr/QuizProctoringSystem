using Microsoft.EntityFrameworkCore;
using QPS.Domain.Entities;
using QPS.Domain.Enums;
using DayOfWeek = QPS.Domain.Enums.DayOfWeek;

namespace QPS.Data;

public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        if (await context.Users.AnyAsync())
            return;

        // Courses
        var courses = new List<Course>
        {
            new() { Id = Guid.NewGuid(), Name = "Cloud Computing", Department = "CS", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Data Structures", Department = "CS", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Operating Systems", Department = "CS", CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Database Systems", Department = "CS", CreatedAt = DateTime.UtcNow },
        };
        context.Courses.AddRange(courses);

        // Semester
        var semester = new Semester
        {
            Id = Guid.NewGuid(),
            Name = "Fall 2026",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 31),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Semesters.Add(semester);

        // Admin user
        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@qps.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
            FirstName = "Admin",
            LastName = "User",
            Role = UserRole.Admin,
            IsActive = true,
            TargetWorkload = 0,
            MaxProctoringSessionsPerSemester = 0,
            CreatedAt = DateTime.UtcNow
        };

        // TA users
        var tas = new List<User>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Email = "ahmed@qps.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                FirstName = "Ahmed",
                LastName = "Ali",
                Role = UserRole.TA,
                DayOff = DayOfWeek.Friday,
                TargetWorkload = 14,
                MaxProctoringSessionsPerSemester = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "sara@qps.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                FirstName = "Sara",
                LastName = "Mohammed",
                Role = UserRole.TA,
                DayOff = DayOfWeek.Saturday,
                TargetWorkload = 12,
                MaxProctoringSessionsPerSemester = 8,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "omar@qps.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                FirstName = "Omar",
                LastName = "Hassan",
                Role = UserRole.TA,
                DayOff = DayOfWeek.Sunday,
                TargetWorkload = 14,
                MaxProctoringSessionsPerSemester = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "fatima@qps.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                FirstName = "Fatima",
                LastName = "Khalil",
                Role = UserRole.TA,
                DayOff = DayOfWeek.Monday,
                TargetWorkload = 10,
                MaxProctoringSessionsPerSemester = 6,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
        };
        context.Users.Add(admin);
        context.Users.AddRange(tas);

        // Sample quizzes
        var quizzes = new List<Quiz>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CourseId = courses[0].Id,
                SemesterId = semester.Id,
                QuizDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
                StartTime = new TimeOnly(8, 30),
                EndTime = new TimeOnly(10, 0),
                WeekNumber = 9,
                SlotNumber = 1,
                Group = "GPI",
                Status = QuizStatus.Upcoming,
                AutoAssign = false,
                AddBackup = true,
                CreatedAt = DateTime.UtcNow,
                Locations = new List<QuizLocation>
                {
                    new() { Id = Guid.NewGuid(), RoomName = "M1.105", ProctorsNeeded = 1, CreatedAt = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), RoomName = "M1.205", ProctorsNeeded = 1, CreatedAt = DateTime.UtcNow }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                CourseId = courses[1].Id,
                SemesterId = semester.Id,
                QuizDate = DateOnly.FromDateTime(DateTime.Today.AddDays(5)),
                StartTime = new TimeOnly(15, 45),
                EndTime = new TimeOnly(17, 15),
                WeekNumber = 9,
                SlotNumber = 5,
                Group = "GPII",
                Status = QuizStatus.Upcoming,
                AutoAssign = false,
                AddBackup = false,
                CreatedAt = DateTime.UtcNow,
                Locations = new List<QuizLocation>
                {
                    new() { Id = Guid.NewGuid(), RoomName = "C.101", ProctorsNeeded = 1, CreatedAt = DateTime.UtcNow }
                }
            },
            new()
            {
                Id = Guid.NewGuid(),
                CourseId = courses[2].Id,
                SemesterId = semester.Id,
                QuizDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
                StartTime = new TimeOnly(13, 45),
                EndTime = new TimeOnly(15, 15),
                WeekNumber = 10,
                SlotNumber = 4,
                Group = "GPIII",
                Status = QuizStatus.Upcoming,
                AutoAssign = false,
                AddBackup = true,
                CreatedAt = DateTime.UtcNow,
                Locations = new List<QuizLocation>
                {
                    new() { Id = Guid.NewGuid(), RoomName = "A.020", ProctorsNeeded = 1, CreatedAt = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), RoomName = "A.128", ProctorsNeeded = 1, CreatedAt = DateTime.UtcNow },
                    new() { Id = Guid.NewGuid(), RoomName = "A.228", ProctorsNeeded = 1, CreatedAt = DateTime.UtcNow }
                }
            },
        };
        context.Quizzes.AddRange(quizzes);

        // Sample schedule slots for TAs
        var scheduleSlots = new List<TAScheduleSlot>
        {
            new() { Id = Guid.NewGuid(), UserId = tas[0].Id, SemesterId = semester.Id, DayOfWeek = DayOfWeek.Saturday, SlotNumber = 1, CourseName = "Cloud Computing - Lab", SlotType = SlotType.Lab, Room = "M031", StartTime = new TimeOnly(8, 30), EndTime = new TimeOnly(10, 0), CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), UserId = tas[0].Id, SemesterId = semester.Id, DayOfWeek = DayOfWeek.Monday, SlotNumber = 3, CourseName = "Cloud Computing - Tut", SlotType = SlotType.Tut, Room = "A101", StartTime = new TimeOnly(12, 0), EndTime = new TimeOnly(13, 30), CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), UserId = tas[1].Id, SemesterId = semester.Id, DayOfWeek = DayOfWeek.Sunday, SlotNumber = 2, CourseName = "Data Structures - Lab", SlotType = SlotType.Lab, Room = "B205", StartTime = new TimeOnly(10, 15), EndTime = new TimeOnly(11, 45), CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), UserId = tas[1].Id, SemesterId = semester.Id, DayOfWeek = DayOfWeek.Tuesday, SlotNumber = 4, CourseName = "Data Structures - Tut", SlotType = SlotType.Tut, Room = "B205", StartTime = new TimeOnly(13, 45), EndTime = new TimeOnly(15, 15), CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), UserId = tas[2].Id, SemesterId = semester.Id, DayOfWeek = DayOfWeek.Monday, SlotNumber = 1, CourseName = "Operating Systems - Lab", SlotType = SlotType.Lab, Room = "C302", StartTime = new TimeOnly(8, 30), EndTime = new TimeOnly(10, 0), CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), UserId = tas[2].Id, SemesterId = semester.Id, DayOfWeek = DayOfWeek.Wednesday, SlotNumber = 5, CourseName = "Operating Systems - Tut", SlotType = SlotType.Tut, Room = "C302", StartTime = new TimeOnly(15, 45), EndTime = new TimeOnly(17, 15), CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), UserId = tas[3].Id, SemesterId = semester.Id, DayOfWeek = DayOfWeek.Tuesday, SlotNumber = 1, CourseName = "Database Systems - Lab", SlotType = SlotType.Lab, Room = "D404", StartTime = new TimeOnly(8, 30), EndTime = new TimeOnly(10, 0), CreatedAt = DateTime.UtcNow },
        };
        context.TAScheduleSlots.AddRange(scheduleSlots);

        await context.SaveChangesAsync();
    }
}
