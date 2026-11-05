using Microsoft.EntityFrameworkCore;
using QPS.Application.Common.Services;
using QPS.Domain.Entities;
using QPS.Domain.Enums;
using QPS.Domain.Rules;
using QPS.Domain.Scheduling;
using DayOfWeek = QPS.Domain.Enums.DayOfWeek;

namespace QPS.Data;

/// <summary>
/// Demo data for a fresh database. Everything is generated relative to "today" so the demo
/// always has finished quizzes, quizzes this week and unstaffed quizzes later in the semester.
/// Past assignments are produced by the real assignment engine, so they obey every rule.
/// </summary>
public static class SeedData
{
    public const string DemoPassword = "password123";

    public static async Task InitializeAsync(AppDbContext db, DateOnly today)
    {
        if (await db.Users.AnyAsync()) return;

        var random = new Random(20260916);
        var hash = BCrypt.Net.BCrypt.HashPassword(DemoPassword);
        var settings = new SystemSettings { Id = Guid.NewGuid() };
        db.SystemSettings.Add(settings);

        // ── Semester: today falls in academic week 7; weeks run Tuesday → Monday like the university calendar.
        var lastTuesday = today.AddDays(-(((int)today.DayOfWeek - (int)System.DayOfWeek.Tuesday + 7) % 7));
        var semesterStart = lastTuesday.AddDays(-6 * 7);
        var semester = new Semester
        {
            Id = Guid.NewGuid(),
            Name = SemesterName(today),
            StartDate = semesterStart,
            EndDate = semesterStart.AddDays(15 * 7 - 1),
            IsActive = true
        };
        db.Semesters.Add(semester);

        // ── Courses
        var courseNames = new[]
        {
            "Computer Networks", "Theoretical Computer Science", "Mathematics", "Computer Organization",
            "Programming II", "Cloud Computing", "Network Security", "Information Security",
            "Introduction to Data Science", "Advanced Machine Learning", "Distributed & Web-based Systems",
            "Analysis and Design of Algorithms", "Database Systems", "Operating Systems",
            "Big Data and NoSQL", "Introduction to Media Informatics"
        };
        var courses = courseNames.Select(n => new Course
        {
            Id = Guid.NewGuid(), Name = n, NormalizedName = NameKey.For(n), Department = settings.DepartmentName
        }).ToList();
        db.Courses.AddRange(courses);

        // ── Rooms (bigger halls need more proctors)
        var roomSpecs = new (string Name, int Capacity, int Proctors)[]
        {
            ("C.101", 60, 2), ("C.102", 60, 2), ("C.201", 90, 3), ("C.202", 60, 2),
            ("A.020", 120, 3), ("A.128", 60, 2), ("A.228", 60, 2), ("A2.101", 45, 2), ("A2.201", 45, 2),
            ("M1.105", 50, 2), ("M1.205", 50, 2), ("S1.006", 140, 4), ("S1.107", 60, 2),
            ("S3.431", 40, 2), ("S3.435", 40, 2), ("0.14", 80, 3), ("0.31", 80, 3)
        };
        var rooms = roomSpecs.Select(r => new Room
        {
            Id = Guid.NewGuid(), Name = r.Name, NormalizedName = NameKey.For(r.Name), Capacity = r.Capacity, DefaultProctors = r.Proctors
        }).ToList();
        db.Rooms.AddRange(rooms);

        // ── People
        var admin = NewUser("admin@qps.com", "Admin", "One", UserRole.Admin, hash, settings.DepartmentName);
        var admin2 = NewUser("admin2@qps.com", "Admin", "Two", UserRole.Admin, hash, settings.DepartmentName);
        db.Users.AddRange(admin, admin2);

        var taNames = new (string Email, string First, string Last)[]
        {
            ("ahmed@qps.com", "Ahmed", "Ali"), ("sara@qps.com", "Sara", "Mohammed"),
            ("omar@qps.com", "Omar", "Hassan"), ("fatima@qps.com", "Fatima", "Khalil"),
            ("youssef.nabil@qps.com", "Youssef", "Nabil"), ("mariam.adel@qps.com", "Mariam", "Adel"),
            ("karim.samir@qps.com", "Karim", "Samir"), ("hana.mostafa@qps.com", "Hana", "Mostafa"),
            ("mostafa.gamal@qps.com", "Mostafa", "Gamal"), ("laila.sherif@qps.com", "Laila", "Sherif"),
            ("ziad.hamdy@qps.com", "Ziad", "Hamdy"), ("salma.tarek@qps.com", "Salma", "Tarek"),
            ("hassan.wael@qps.com", "Hassan", "Wael"), ("rana.ehab@qps.com", "Rana", "Ehab"),
            ("amr.sobhy@qps.com", "Amr", "Sobhy"), ("dina.magdy@qps.com", "Dina", "Magdy"),
            ("tarek.fouad@qps.com", "Tarek", "Fouad"), ("yara.ashraf@qps.com", "Yara", "Ashraf"),
            ("khaled.mansour@qps.com", "Khaled", "Mansour"), ("aya.ibrahim@qps.com", "Aya", "Ibrahim"),
            ("mahmoud.reda@qps.com", "Mahmoud", "Reda"), ("nada.hesham@qps.com", "Nada", "Hesham"),
            ("seif.adel@qps.com", "Seif", "Adel"), ("habiba.saeed@qps.com", "Habiba", "Saeed"),
            ("ali.kamal@qps.com", "Ali", "Kamal"), ("malak.hany@qps.com", "Malak", "Hany"),
            ("bassel.ayman@qps.com", "Bassel", "Ayman"), ("jana.wagdy@qps.com", "Jana", "Wagdy"),
            ("adham.lotfy@qps.com", "Adham", "Lotfy"), ("farida.nasser@qps.com", "Farida", "Nasser")
        };

        var working = AcademicCalendar.WorkingDays;
        var tas = new List<User>();
        for (var i = 0; i < taNames.Length; i++)
        {
            var (email, first, last) = taNames[i];
            var ta = NewUser(email, first, last, UserRole.TA, hash, settings.DepartmentName);
            var off = working[random.Next(working.Count)];
            ta.DaysOff = AcademicCalendar.ToFlag(off);
            if (i % 5 == 2)
            {
                var second = working.Where(d => d != off).ElementAt(random.Next(working.Count - 1));
                ta.DaysOff |= AcademicCalendar.ToFlag(second);
            }
            tas.Add(ta);
        }
        tas[7].CanEditSchedule = false;                 // a TA whose schedule is locked
        tas[29].IsActive = false;                       // a deactivated account
        tas[3].TargetWorkloadOverride = 12;            // per-TA overrides (Rule 1 + target)
        tas[3].MaxSessionsOverride = 16;
        db.Users.AddRange(tas);

        // ── Weekly teaching schedules. Each TA teaches two courses; one TA has no schedule yet.
        var slots = new List<TAScheduleSlot>();
        for (var i = 0; i < tas.Count; i++)
        {
            if (i == 27) continue; // shows up under the "No schedule" filter
            var ta = tas[i];
            var taught = new[] { courses[i % courses.Count], courses[(i * 7 + 3) % courses.Count] };
            var days = working.Where(d => !ta.DaysOff.HasFlag(AcademicCalendar.ToFlag(d))).ToList();
            var count = 5 + random.Next(5);
            var used = new HashSet<(DayOfWeek, int)>();
            var guard = 0;
            while (used.Count < count && guard++ < 200)
            {
                var day = days[random.Next(days.Count)];
                var slotNumber = 1 + random.Next(5);
                if (!used.Add((day, slotNumber))) continue;
                var course = taught[random.Next(2)];
                var def = SlotCatalog.Get(slotNumber)!;
                slots.Add(new TAScheduleSlot
                {
                    Id = Guid.NewGuid(),
                    UserId = ta.Id,
                    SemesterId = semester.Id,
                    DayOfWeek = day,
                    SlotNumber = slotNumber,
                    CourseId = course.Id,
                    CourseName = course.Name,
                    SlotType = random.Next(3) == 0 ? SlotType.Tut : SlotType.Lab,
                    Room = rooms[random.Next(rooms.Count)].Name,
                    StartTime = def.Start,
                    EndTime = def.End
                });
            }
        }
        db.TAScheduleSlots.AddRange(slots);

        // ── Quiz calendar for weeks 3–12 (like the university PDF: mostly 5th slot and the gap).
        var groups = new[] { "GPI", "GPII", "GPIII" };
        var quizSlots = new[] { 5, 5, 0, 1, 5, 3, 0, 5 };
        var quizzes = new List<Quiz>();
        var courseCursor = 0;
        for (var week = 3; week <= 12; week++)
        {
            var weekStart = AcademicCalendar.WeekStart(semesterStart, week);
            var weekDays = Enumerable.Range(0, 7).Select(d => weekStart.AddDays(d))
                .Where(d => d.DayOfWeek != System.DayOfWeek.Friday).ToList();
            var sittings = 3 + random.Next(2);
            for (var s = 0; s < sittings; s++)
            {
                var course = courses[courseCursor++ % courses.Count];
                var date = weekDays[(s * 2 + week) % weekDays.Count];
                var slotDef = SlotCatalog.Get(quizSlots[(week + s) % quizSlots.Length])!;
                var isMidterm = week == 8 && s == 0;
                var start = isMidterm ? new TimeOnly(9, 0) : slotDef.Start;
                var end = isMidterm ? new TimeOnly(11, 0) : slotDef.End;
                var quiz = new Quiz
                {
                    Id = Guid.NewGuid(),
                    CourseId = course.Id,
                    Course = course,
                    SemesterId = semester.Id,
                    ExamType = isMidterm ? ExamType.Midterm : ExamType.Quiz,
                    WeekNumber = week,
                    QuizDate = date,
                    StartTime = start,
                    EndTime = end,
                    SlotNumber = SlotCatalog.FromTimes(start, end)?.Number,
                    Source = s % 3 == 2 ? QuizSource.Manual : QuizSource.Import,
                    HasReserve = s % 3 == 0
                };

                var groupCount = 1 + random.Next(3);
                var order = 0;
                var usedRooms = new HashSet<string>();
                for (var g = 0; g < groupCount; g++)
                {
                    var roomsForGroup = 1 + random.Next(2);
                    for (var r = 0; r < roomsForGroup; r++)
                    {
                        var room = rooms[random.Next(rooms.Count)];
                        if (!usedRooms.Add(room.Name)) continue;
                        quiz.Locations.Add(new QuizLocation
                        {
                            Id = Guid.NewGuid(), QuizId = quiz.Id, RoomName = room.Name, Group = groups[g],
                            ProctorsNeeded = room.DefaultProctors, SortOrder = order++
                        });
                    }
                }
                if (quiz.HasReserve)
                    quiz.Locations.Add(new QuizLocation
                    {
                        Id = Guid.NewGuid(), QuizId = quiz.Id, RoomName = "Reserved", ProctorsNeeded = settings.ReserveProctors,
                        IsReserve = true, SortOrder = 999
                    });
                quizzes.Add(quiz);
            }
        }
        db.Quizzes.AddRange(quizzes);

        // ── Run the real engine for weeks 3–9; weeks 10–12 stay empty for the batch run demo.
        var profiles = tas.Where(t => t.IsActive).Select(t => new ProctorProfile
        {
            Id = t.Id,
            Name = t.FullName,
            DaysOff = t.DaysOff,
            MaxSessionsPerSemester = t.MaxSessionsOverride ?? settings.DefaultMaxSessionsPerSemester,
            TargetWorkload = t.TargetWorkloadOverride ?? settings.DefaultTargetWorkload,
            Teaching = slots.Where(s => s.UserId == t.Id)
                .Select(s => new TeachingCommitment(s.DayOfWeek, s.SlotNumber, s.StartTime, s.EndTime, s.CourseName, s.SlotType)).ToList(),
            CourseIds = slots.Where(s => s.UserId == t.Id && s.CourseId.HasValue).Select(s => s.CourseId!.Value).ToHashSet()
        }).ToList();

        var toStaff = quizzes.Where(q => q.WeekNumber <= 9).ToList();
        var demands = toStaff.Select(q => new QuizDemand(
            new QuizSession(q.Id, semester.Id, q.CourseId, q.QuizDate, q.StartTime, q.EndTime, q.WeekNumber),
            q.Course.Name,
            q.Locations.Select(l => new LocationDemand(l.Id, l.RoomName, l.IsReserve, l.SortOrder,
                // leave one seat open in a few upcoming quizzes so "missing proctors" has something to show
                l.ProctorsNeeded - (q.WeekNumber == 9 && !l.IsReserve && l.SortOrder == 0 ? 1 : 0), false)).ToList()))
            .ToList();
        var plan = AssignmentEngine.Plan(demands, profiles, new Dictionary<Guid, List<BookedSession>>(),
            new EngineOptions(new RulesConfig(settings.MaxSessionsPerWeek), settings.PreferCourseTas));

        var quizById = quizzes.ToDictionary(q => q.Id);
        var assignments = plan.Assignments.Select(p => new ProctorAssignment
        {
            Id = Guid.NewGuid(),
            QuizLocationId = p.LocationId,
            UserId = p.UserId,
            IsCourseTa = p.IsCourseTa,
            Source = AssignmentSource.Auto,
            Points = 1,
            CreatedAt = quizById[p.QuizId].QuizDate.AddDays(-12).ToDateTime(new TimeOnly(10, 0), DateTimeKind.Utc)
        }).ToList();
        db.ProctorAssignments.AddRange(assignments);

        // ── Excuses: one TA fell sick after being assigned next week (shows a flagged proctor).
        var nextWeekStart = AcademicCalendar.WeekStart(semesterStart, 8);
        var flaggedPick = assignments
            .Select(a => (a, q: quizById[plan.Assignments.First(p => p.LocationId == a.QuizLocationId && p.UserId == a.UserId).QuizId]))
            .Where(x => x.q.QuizDate >= nextWeekStart && x.q.QuizDate < nextWeekStart.AddDays(7))
            .OrderBy(x => x.q.QuizDate)
            .FirstOrDefault();
        var excuses = new List<Excuse>();
        if (flaggedPick.a is not null)
        {
            excuses.Add(new Excuse
            {
                Id = Guid.NewGuid(), UserId = flaggedPick.a.UserId, Reason = "Medical leave (flu)",
                StartDate = flaggedPick.q.QuizDate.AddDays(-1), EndDate = flaggedPick.q.QuizDate.AddDays(2), SubmittedByTa = true,
                CreatedById = flaggedPick.a.UserId
            });
        }
        excuses.Add(new Excuse
        {
            Id = Guid.NewGuid(), UserId = tas[12].Id, Reason = "Conference travel",
            StartDate = AcademicCalendar.WeekStart(semesterStart, 10), EndDate = AcademicCalendar.WeekStart(semesterStart, 10).AddDays(4),
            SubmittedByTa = false, CreatedById = admin.Id
        });
        excuses.Add(new Excuse
        {
            Id = Guid.NewGuid(), UserId = tas[5].Id, Reason = "Family emergency",
            StartDate = AcademicCalendar.WeekStart(semesterStart, 2), EndDate = AcademicCalendar.WeekStart(semesterStart, 2).AddDays(3),
            SubmittedByTa = true, CreatedById = tas[5].Id
        });
        db.Excuses.AddRange(excuses);

        // ── A few notifications so the bell isn't empty.
        var stamp = DateTime.UtcNow;
        void Note(User to, string message, NotificationType type, string? link, int minutesAgo, bool read = false) =>
            db.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(), UserId = to.Id, Message = message, Type = type, Link = link, IsRead = read,
                CreatedAt = stamp.AddMinutes(-minutesAgo)
            });

        foreach (var a in new[] { admin, admin2 })
        {
            if (flaggedPick.a is not null)
            {
                var sick = tas.First(t => t.Id == flaggedPick.a.UserId);
                Note(a, $"TA {sick.FullName} submitted an excuse for {Format.Date(flaggedPick.q.QuizDate.AddDays(-1))} – {Format.Date(flaggedPick.q.QuizDate.AddDays(2))}: Medical leave (flu). 1 upcoming proctoring session needs a replacement.",
                    NotificationType.Excuse, $"/proctors?ta={sick.Id}", 35);
            }
            Note(a, $"TA {tas[1].FullName} added a slot on Thursday at Slot 2.", NotificationType.Schedule, $"/schedules?ta={tas[1].Id}", 90);
            Note(a, $"TA {tas[9].FullName} removed a slot on Sunday at Slot 3.", NotificationType.Schedule, $"/schedules?ta={tas[9].Id}", 180);
            Note(a, "Quiz calendar imported: 12 new quizzes, 0 updated.", NotificationType.Quiz, "/quizzes", 60 * 26, read: true);
        }
        Note(tas[0], "Welcome! Your proctoring sessions and schedule are on your home page.", NotificationType.General, "/", 60 * 24 * 3, read: true);

        await db.SaveChangesAsync();
    }

    private static User NewUser(string email, string first, string last, UserRole role, string hash, string department) => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        PasswordHash = hash,
        FirstName = first,
        LastName = last,
        Role = role,
        Department = department,
        IsActive = true,
        CanEditSchedule = true
    };

    private static string SemesterName(DateOnly today) => today.Month switch
    {
        >= 9 => $"Winter {today.Year}",
        1 => $"Winter {today.Year - 1}",
        >= 2 and <= 6 => $"Spring {today.Year}",
        _ => $"Summer {today.Year}"
    };
}
