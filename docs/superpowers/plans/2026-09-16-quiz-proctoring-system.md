# Quiz Proctoring System — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a complete Quiz Proctoring System with ASP.NET Core backend, React frontend, and auto-assignment algorithm for assigning TAs to exam proctoring sessions.

**Architecture:** Clean Architecture with CQRS pattern using MediatR. Backend handles business logic, frontend is a React SPA. SQLite database with EF Core for data access. JWT authentication for security.

**Tech Stack:** .NET 8, ASP.NET Core, MediatR, FluentValidation, EF Core (SQLite), React 19, Vite, TypeScript, ClosedXML (Excel), BCrypt (passwords)

**Spec:** `docs/superpowers/specs/2026-09-16-quiz-proctoring-system-design.md`

## Global Constraints

- .NET 8 SDK required
- Node.js 18+ for frontend
- SQLite for database (swappable to PostgreSQL later)
- BCrypt for password hashing
- JWT Bearer authentication
- Follow Clean Architecture dependency rules
- All entities inherit from BaseEntity (Id, CreatedAt, UpdatedAt, soft delete)
- CQRS pattern: Commands for writes, Queries for reads
- FluentValidation for all request validation
- Result pattern for error handling (no exceptions for expected failures)

---

## Phase 1: Backend Foundation (Tasks 1-5)

### Task 1: Solution Scaffolding
- Create solution and 5 projects (Domain, Application, Infrastructure, Data, API)
- Add project references and NuGet packages
- **Files:** Solution file, 5 .csproj files

### Task 2: Domain Entities & Enums
- Create BaseEntity, User, Course, Semester, Quiz, QuizLocation, ProctorAssignment, TAScheduleSlot, Notification, Excuse, WorkloadConfig
- Create enums: UserRole, DayOfWeek, QuizStatus, AssignmentStatus, SlotType
- **Files:** 11 entity files, 5 enum files

### Task 3: Application Layer
- Create IApplicationDbContext, ICurrentUserService, IExcelParserService, IProctorAssignmentService, INotificationService
- Create Result<T>, PaginatedList<T>, ValidationBehavior
- Create DependancyInjection
- **Files:** 5 interfaces, 3 model files, 1 behavior, 1 DI file

### Task 4: Data Layer
- Create AppDbContext with entity configurations
- Create DependancyInjection
- **Files:** AppDbContext.cs, DependancyInjection.cs

### Task 5: Infrastructure Services
- Create TokenService, CurrentUserService, NotificationService
- Create DependancyInjection
- **Files:** 3 services, 1 DI file

---

## Phase 2: API Layer (Tasks 6-7)

### Task 6: API Configuration
- Configure Program.cs (JWT, CORS, DI, middleware)
- Create ExceptionHandlingMiddleware
- Configure appsettings.json
- **Files:** Program.cs, ExceptionHandlingMiddleware.cs, appsettings.json

### Task 7: Auth Endpoints
- Register (email/password, BCrypt)
- Login (JWT token)
- GetCurrentUser
- **Files:** AuthController.cs, Register.cs, Login.cs, GetCurrentUser.cs, AuthDtos.cs

---

## Phase 3: Core Features (Tasks 8-13)

### Task 8: Course Management
- CRUD for courses (pre-defined list, admin-managed)
- **Files:** CoursesController.cs, CreateCourse.cs, DeleteCourse.cs, GetCourses.cs, CourseDtos.cs

### Task 9: Semester Management
- CRUD for semesters (active semester concept)
- **Files:** SemestersController.cs, CreateSemester.cs, UpdateSemester.cs, GetSemesters.cs, SemesterDtos.cs

### Task 10: Quiz Management
- Create quiz manually (course, date, time, locations)
- Import quizzes from Excel
- Get quizzes (filtered by semester, status)
- Auto-update status based on date
- **Files:** QuizzesController.cs, CreateQuiz.cs, ImportQuizzes.cs, GetQuizzes.cs, QuizDtos.cs

### Task 11: TA Schedule Management
- Slot-based schedules (5 fixed time slots)
- CRUD for schedule slots
- Day off configuration
- **Files:** SchedulesController.cs, CreateScheduleSlot.cs, DeleteScheduleSlot.cs, GetSchedules.cs, ScheduleDtos.cs

### Task 12: Proctor Assignment
- Auto-assign algorithm (greedy)
- Manual assign
- Get assignments
- **Files:** AssignmentsController.cs, AutoAssign.cs, ManualAssign.cs, GetAssignments.cs, AssignmentDtos.cs, ProctorAssignmentService.cs

### Task 13: User Management
- Create TAs (admin)
- Get users (filtered by role)
- Set day off, target workload, max sessions
- **Files:** UsersController.cs, CreateUser.cs, GetUsers.cs, UserDtos.cs

---

## Phase 4: Supporting Features (Tasks 14-15)

### Task 14: Notifications
- In-app notifications for schedule changes
- Mark as read / Mark all as read
- **Files:** NotificationsController.cs, GetNotifications.cs, MarkAsRead.cs, NotificationDtos.cs

### Task 15: Dashboard & Analytics
- Summary stats (active TAs, upcoming quizzes, missing proctors)
- Analytics (total quizzes, assignments, average per TA)
- Most active TAs leaderboard
- **Files:** DashboardController.cs, GetDashboardSummary.cs, GetAnalytics.cs, DashboardDtos.cs

---

## Phase 5: Frontend (Tasks 16-20)

### Task 16: Frontend Scaffolding
- Create Vite + React + TypeScript project
- Configure routing, API service, auth context
- **Files:** package.json, vite.config.ts, App.tsx, api.ts, AuthContext.tsx

### Task 17: Auth & Dashboard Pages
- Login page
- Dashboard (stats, most active TAs)
- Sidebar component
- **Files:** Login.tsx, Dashboard.tsx, Sidebar.tsx

### Task 18: Quiz Pages
- All Quizzes (grouped by date, filters)
- Add Quiz (form with locations)
- **Files:** Quizzes.tsx, AddQuiz.tsx

### Task 19: Schedule & Assignment Pages
- TA Schedules (slot-based view)
- Assignments (list, auto-assign trigger)
- **Files:** Schedules.tsx, Assignments.tsx

### Task 20: Reporting Pages
- Proctor Summary (TA list, workload)
- Reviewing/Summary (TAs tab, Courses tab, Analytics tab)
- **Files:** ProctorSummary.tsx, Reviewing.tsx

---

## Implementation Order

1. **Phase 1** (Backend Foundation) — Days 1-2
2. **Phase 2** (API Layer) — Day 3
3. **Phase 3** (Core Features) — Days 4-7
4. **Phase 4** (Supporting Features) — Day 8
5. **Phase 5** (Frontend) — Days 9-12

---

## Testing Strategy

- Unit tests for handlers and validators
- Integration tests for API endpoints
- Manual testing of auto-assignment algorithm
- Frontend testing with React Testing Library

---

## Notes

- The plan follows Clean Architecture patterns from the template README
- Each task produces an independently testable deliverable
- The auto-assignment algorithm implements the 3 mandatory rules
- Excel import uses ClosedXML library
- Frontend uses Tailwind CSS for styling
