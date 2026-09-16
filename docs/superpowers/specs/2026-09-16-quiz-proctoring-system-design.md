# Quiz Proctoring System — Design Specification (v2)

## 1. Overview

A system for assigning teaching assistants and proctors to exam proctoring sessions based on their availability, schedule constraints, and university rules. Admins import exam schedules from Excel OR create quizzes manually, manage TA schedules with a slot-based system, and the system auto-assigns proctors with an admin review step.

### Goals
- Automate proctor assignment based on TA availability and constraints
- Ensure compliance with mandatory rules (max sessions, weekly limit, no conflicts)
- Provide admin with a review/edit workflow before assignments are finalized
- Support both Excel import and manual quiz creation
- Follow Clean Architecture + CQRS pattern (template-based)

---

## 2. Tech Stack

| Layer | Technology |
|---|---|
| Backend | .NET 8 / ASP.NET Core |
| Architecture | Clean Architecture, CQRS |
| Mediator | MediatR |
| Validation | FluentValidation |
| Database | SQLite via EF Core 8.0 |
| Auth | JWT Bearer (email/password, BCrypt) |
| Frontend | React 19 + Vite + TypeScript |
| Excel Parsing | ClosedXML |

---

## 3. Domain Model

### 3.1 Entities

**User** (extends BaseEntity)
| Field | Type | Notes |
|---|---|---|
| Email | string | Unique, required |
| PasswordHash | string | BCrypt hashed |
| FirstName | string | Required |
| LastName | string | Required |
| Role | UserRole enum | Admin, TA |
| DayOff | DayOfWeek? | TA's additional day off (Friday mandatory for all) |
| TargetWorkload | int | Goal proctoring points per semester |
| MaxProctoringSessionsPerSemester | int | Hard limit, configurable per TA |
| IsActive | bool | Default true |
| Soft delete fields | | IsDeleted, DeletedAt, DeletedBy |

**Course** (extends BaseEntity)
| Field | Type | Notes |
|---|---|---|
| Name | string | e.g., "Cloud Computing" |
| Department | string | e.g., "CS" |
| CreatedAt | DateTime | |

**Semester** (extends BaseEntity)
| Field | Type | Notes |
|---|---|---|
| Name | string | e.g., "Fall 2026" |
| StartDate | DateOnly | |
| EndDate | DateOnly | |
| IsActive | bool | Only one active at a time |

**Quiz** (extends BaseEntity)
| Field | Type | Notes |
|---|---|---|
| CourseId | Guid (FK → Course) | |
| SemesterId | Guid (FK → Semester) | |
| QuizDate | DateOnly | |
| StartTime | TimeOnly | |
| EndTime | TimeOnly | |
| Status | QuizStatus enum | Upcoming, Started, Finished |
| AutoAssign | bool | Auto-assign proctors on creation |
| AddBackup | bool | Add backup locations automatically |
| CreatedAt | DateTime | |

**QuizLocation** (extends BaseEntity)
| Field | Type | Notes |
|---|---|---|
| QuizId | Guid (FK → Quiz) | |
| RoomName | string | e.g., "M031", "A101" |
| ProctorsNeeded | int | Per-room proctor count |
| CreatedAt | DateTime | |

**ProctorAssignment** (extends BaseEntity)
| Field | Type | Notes |
|---|---|---|
| QuizLocationId | Guid (FK → QuizLocation) | |
| UserId | Guid (FK → User) | Assigned TA |
| IsBackup | bool | Backup proctor flag |
| Status | AssignmentStatus enum | Assigned, Confirmed, Cancelled |
| CreatedAt | DateTime | |

**TAScheduleSlot** (extends BaseEntity)
| Field | Type | Notes |
|---|---|---|
| UserId | Guid (FK → User) | The TA |
| SemesterId | Guid (FK → Semester) | |
| DayOfWeek | DayOfWeek enum | |
| SlotNumber | int | 1-5 (fixed time slots) |
| CourseName | string | Course they're teaching |
| SlotType | SlotType enum | Lab, Tut |
| Room | string | Room location |
| StartTime | TimeOnly | From fixed slot schedule |
| EndTime | TimeOnly | From fixed slot schedule |

**Notification** (extends BaseEntity)
| Field | Type | Notes |
|---|---|---|
| UserId | Guid (FK → User) | Recipient |
| Message | string | |
| IsRead | bool | Default false |
| CreatedAt | DateTime | |

**Excuse** (extends BaseEntity)
| Field | Type | Notes |
|---|---|---|
| UserId | Guid (FK → User) | The TA |
| Reason | string | |
| StartDate | DateOnly | |
| EndDate | DateOnly | |
| IsActive | bool | |
| CreatedAt | DateTime | |

**WorkloadConfig** (extends BaseEntity)
| Field | Type | Notes |
|---|---|---|
| TargetWorkload | int | Default target (e.g., 14) |
| UpdatedAt | DateTime | |

### 3.2 Enums

```
UserRole: Admin = 1, TA = 2
DayOfWeek: Friday = 0, Saturday = 1, Sunday = 2, Monday = 3, Tuesday = 4, Wednesday = 5, Thursday = 6
ExamType: Quiz = 0, Midterm = 1, Final = 2
QuizStatus: Upcoming = 0, Started = 1, Finished = 2
AssignmentStatus: Assigned = 0, Confirmed = 1, Cancelled = 2
SlotType: Lab = 0, Tut = 1
```

### 3.3 Fixed Slot Times

| Slot | Start Time | End Time |
|---|---|---|
| 1 | 8:30 AM | 10:00 AM |
| 2 | 10:15 AM | 11:45 AM |
| 3 | 12:00 PM | 1:30 PM |
| 4 | 1:45 PM | 3:15 PM |
| 5 | 3:45 PM | 5:15 PM |

### 3.4 TA Days Off Rules
- **Friday**: Mandatory off for ALL TAs
- **Additional days**: Each TA has 1-2 extra days off per week (configured per TA)
- TAs typically have 2 days off per week total

---

## 4. Mandatory Rules

### Rule 1: Max Sessions Per Semester (Hard Limit)
- Global default configurable by admin
- Per-TA override available
- Counted per semester; resets each semester
- System BLOCKS assignment if exceeded

### Rule 2: Max 2 Sessions Per Week
- Checked against the week containing the quiz (Monday-Sunday)
- Simple count of assignments in that week
- System BLOCKS assignment if exceeded

### Rule 3: No Schedule Conflict
- For each quiz, check if TA has a schedule slot on the same day
- TAs are NOT available on their days off (Friday mandatory + configured days)
- Overlap exists if: `Slot.StartTime < Quiz.EndTime AND Slot.EndTime > Quiz.StartTime`

### Workload Tracking (Soft Limit)
- Each assignment = 1 point
- Target workload is a goal, not a hard limit
- Admin sees current vs. target workload for each TA
- System warns when TA exceeds target but doesn't block

### Enforcement Points
- Auto-assignment: only eligible TAs considered
- Manual assignment: system warns if rules violated
- Admin can override with force-assign

---

## 5. Quiz Creation

### 5.1 Manual Quiz Creation (Like Demo)

**Form fields:**
- Course Name (searchable dropdown from pre-defined courses)
- Quiz Date (calendar picker)
- Start Time (time picker)
- End Time (time picker)
- Locations (multiple):
  - Location Name (text input)
  - Proctors Needed (2-5 selector)
  - "+ Add Another Location" button
- Auto-assign Proctors toggle
- Add automatic backup toggle (creates backup locations)

### 5.2 Excel Import

**Fixed format columns:**
| Column | Required | Default |
|---|---|---|
| Course Name | Yes | — |
| Quiz Date | Yes | — |
| Start Time | Yes | — |
| End Time | Yes | — |
| Room | Yes | — |
| Proctors Needed | No | 1 |

**Import flow:**
1. Admin uploads Excel file
2. System validates columns and parses rows
3. Creates Quiz + QuizLocation records (status: Upcoming)
4. Admin reviews before confirming
5. Auto-assignment runs if enabled

### 5.3 Quiz Status Flow
- **Upcoming**: Quiz is in the future
- **Started**: Quiz date/time has arrived (auto or manual)
- **Finished**: Quiz is complete (auto or manual)
- Status auto-updates based on current date/time
- Admin can manually change status if needed

---

## 6. TA Schedule Management

### Slot-Based System
- TAs have a weekly schedule using fixed time slots (1-5)
- Each slot shows: course name, type (Lab/Tut), room, time
- Admin manages TA schedules
- "Allow Editing" toggle lets TAs edit their own schedule

### Schedule Entry Fields
- Day of week
- Slot number (1-5, maps to fixed times)
- Course name
- Type (Lab or Tut)
- Room

### Day Off Configuration
- Friday is mandatory off for all TAs
- Admin configures additional day(s) off per TA
- TAs cannot be assigned to proctor on their days off

---

## 7. Auto-Assignment Algorithm (Greedy v1)

### Input
- Quizzes sorted by date ASC, then start time ASC
- TAs with schedules, limits, and current assignments

### Algorithm
```
FOR each quiz:
    FOR each location in quiz.locations:
        eligible_tas = []
        FOR each TA:
            IF rule1_pass(TA, quiz)           # Max sessions not exceeded
               AND rule2_pass(TA, quiz)       # Weekly limit not exceeded
               AND rule3_pass(TA, quiz)       # No schedule conflict
               AND NOT on_day_off(TA, quiz):  # Not on day off
                ADD TA to eligible_tas
        
        SORT eligible_tas by fewest assignments first (workload balancing)
        
        ASSIGN first N TAs (N = location.proctorsNeeded)
        IF quiz.addBackup:
            ASSIGN 1 additional backup TA per location
        
        UPDATE counts
```

### Admin Review
- Table view: Quiz | Date | Time | Location | Assigned TAs | Status
- Color-coded: green (full), yellow (partial), red (unstaffed)
- Admin can swap TAs or re-run for specific quizzes/locations

---

## 8. API Endpoints

### Auth
- `POST /api/auth/register` — Register (BCrypt)
- `POST /api/auth/login` — Login (returns JWT)
- `GET /api/auth/me` — Current user

### Semesters
- `GET /api/semesters` — List
- `POST /api/semesters` — Create (admin)
- `PUT /api/semesters/{id}` — Update (admin)

### Courses
- `GET /api/courses` — List (filtered by department)
- `POST /api/courses` — Create (admin)
- `DELETE /api/courses/{id}` — Delete (admin)

### Quizzes
- `GET /api/quizzes` — List (filtered by semester, status)
- `POST /api/quizzes` — Create manually
- `POST /api/quizzes/import` — Import from Excel
- `PUT /api/quizzes/{id}` — Update
- `DELETE /api/quizzes/{id}` — Delete

### Quiz Locations
- `POST /api/quizzes/{id}/locations` — Add location
- `PUT /api/locations/{id}` — Update location
- `DELETE /api/locations/{id}` — Remove location

### TA Schedules
- `GET /api/schedules` — List (filtered by semester, TA)
- `POST /api/schedules` — Add slot
- `PUT /api/schedules/{id}` — Update slot
- `DELETE /api/schedules/{id}` — Remove slot
- `PUT /api/schedules/allow-editing/{taId}` — Toggle editing permission

### Assignments
- `GET /api/assignments` — List (filtered by quiz, TA)
- `POST /api/assignments/auto-assign` — Run algorithm for quiz
- `POST /api/assignments` — Manual assign
- `PUT /api/assignments/{id}` — Modify
- `DELETE /api/assignments/{id}` — Remove
- `GET /api/assignments/my` — TA's own assignments

### Notifications
- `GET /api/notifications` — List user's notifications
- `PUT /api/notifications/{id}/read` — Mark as read
- `PUT /api/notifications/read-all` — Mark all as read

### Dashboard/Analytics
- `GET /api/dashboard/summary` — Stats (active TAs, upcoming quizzes, missing proctors)
- `GET /api/dashboard/most-active` — Top TAs by assignment count
- `GET /api/dashboard/workload` — Per-TA workload (current vs target)
- `GET /api/dashboard/analytics` — Scatter plot data, averages

### Excuses
- `GET /api/excuses` — List active excuses
- `POST /api/excuses` — Add excuse (admin)
- `PUT /api/excuses/{id}` — Update excuse
- `DELETE /api/excuses/{id}` — Remove excuse

### Users (TA Management)
- `GET /api/users` — List users (filtered by role)
- `POST /api/users` — Create TA
- `PUT /api/users/{id}` — Update TA (day off, target, max sessions)

---

## 9. Frontend Pages

### Admin
- `/login` — Login
- `/dashboard` — Stats cards, upcoming schedule, most active TAs, notifications
- `/quizzes` — All quizzes grouped by date, filters (Upcoming/Started/Finished/Incomplete)
- `/quizzes/add` — Create quiz form (course, date, time, locations)
- `/schedules` — TA schedules list, weekly slot view, allow editing toggle
- `/proctor-summary` — TA list, workload stats, assignment history
- `/reviewing` — Tabs: TAs (workload cards), Courses (quiz stats), Analytics (charts)

### TA
- `/login` — Login
- `/my-schedule` — Weekly availability (if editing allowed)
- `/my-assignments` — Own proctoring assignments
- `/profile` — Profile

---

## 10. Project Structure

```
src/
  QPS.Domain/
    Entities/
      BaseEntity.cs
      User.cs
      Course.cs
      Semester.cs
      Quiz.cs
      QuizLocation.cs
      ProctorAssignment.cs
      TAScheduleSlot.cs
      Notification.cs
      Excuse.cs
      WorkloadConfig.cs
    Enums/
      UserRole.cs
      DayOfWeek.cs
      ExamType.cs
      QuizStatus.cs
      AssignmentStatus.cs
      SlotType.cs

  QPS.Application/
    DependancyInjection.cs
    Common/
      Interfaces/
        IApplicationDbContext.cs
        ICurrentUserService.cs
        IExcelParserService.cs
        IProctorAssignmentService.cs
        INotificationService.cs
      Models/
        Result.cs
        PaginatedList.cs
      Behaviors/
        ValidationBehavior.cs
    Features/
      Auth/ (DTOs, Commands, Queries)
      Semesters/ (DTOs, Commands, Queries)
      Courses/ (DTOs, Commands, Queries)
      Quizzes/ (DTOs, Commands, Queries)
      Schedules/ (DTOs, Commands, Queries)
      Assignments/ (DTOs, Commands, Queries)
      Notifications/ (DTOs, Commands, Queries)
      Excuses/ (DTOs, Commands, Queries)
      Dashboard/ (Queries)
      Users/ (DTOs, Commands, Queries)

  QPS.Infrastructure/
    DependancyInjection.cs
    Services/
      TokenService.cs
      CurrentUserService.cs
      ExcelParserService.cs
      ProctorAssignmentService.cs
      NotificationService.cs

  QPS.Data/
    AppDbContext.cs
    Migrations/

  QPS.API/
    Program.cs
    Controllers/
      AuthController.cs
      SemestersController.cs
      CoursesController.cs
      QuizzesController.cs
      SchedulesController.cs
      AssignmentsController.cs
      NotificationsController.cs
      ExcusesController.cs
      DashboardController.cs
      UsersController.cs
    Middleware/
      ExceptionHandlingMiddleware.cs

  qps-client/
    src/
      components/
      pages/
      services/
      App.tsx
```

---

## 11. Open Questions (for Supervisor)

See `docs/supervisor-questions.md` for the full list of unresolved questions including:
1. Exact Excel column format from university
2. How proctor count per exam is determined
3. Excuses system workflow (TA request vs admin add)
4. Professor role (optional)
5. Export/report requirements

---

## 12. Future Enhancements (v2+)

- Constraint-satisfaction optimization algorithm
- Email notifications (in addition to in-app)
- Excel export of assignments
- Professor role (optional)
- Room capacity-based proctor calculation
- Conflict-of-interest detection (TA not proctoring courses they TA)
- Mobile app for TAs to view assignments
