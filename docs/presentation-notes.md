# Quiz Proctoring System — Presentation Notes

## What We Built

A web-based system for automating the assignment of Teaching Assistants (TAs) to exam proctoring sessions. The system handles scheduling, constraint checking, and workload management.

---

## Technical Architecture

### Backend (.NET 8 / ASP.NET Core)
- **Clean Architecture** with 5 layers: Domain, Application, Infrastructure, Data, API
- **CQRS Pattern** (Command Query Responsibility Segregation) — separates read and write operations
- **MediatR** — mediator pattern for handling requests
- **FluentValidation** — input validation for all commands
- **Entity Framework Core** — ORM for database access
- **SQLite** — lightweight database (can migrate to PostgreSQL later)
- **JWT Authentication** — secure token-based login

### Frontend (React 19 + TypeScript)
- **Vite** — fast build tool
- **Tailwind CSS** — utility-first styling
- **React Router** — client-side routing
- **Axios** — HTTP client with JWT interceptors

### Project Structure
```
src/
├── QPS.Domain/          # Entities, Enums, business rules
├── QPS.Application/     # Features (CQRS), interfaces, validation
├── QPS.Infrastructure/  # Services (auth, notifications, proctor assignment)
├── QPS.Data/            # Database context, migrations, seed data
├── QPS.API/             # Controllers, middleware, configuration
└── client/              # React frontend
```

---

## Features Implemented

### 1. Authentication & Authorization
- Email/password login with BCrypt password hashing
- JWT token-based authentication
- Role-based access control (Admin vs TA)
- Protected API endpoints

### 2. Course Management
- Pre-defined courses per department (e.g., "Cloud Computing", "Data Structures")
- Admin can add new courses
- Courses are stored as separate entities

### 3. Semester Management
- Create semesters with start/end dates
- One active semester at a time
- All data (quizzes, schedules) linked to semesters

### 4. Quiz/Exam Management
- **Manual creation**: Admin creates quizzes via form (course, date, time, locations)
- **Excel import**: Upload Excel file to bulk-import quizzes
- Multiple locations per quiz (e.g., Room M031 needs 2 proctors, Room A101 needs 1)
- Quiz status tracking (Upcoming, Started, Finished)

### 5. TA Schedule Management
- **5 fixed time slots** per day (90 minutes each with 15-minute breaks):
  - Slot 1: 8:30 AM - 10:00 AM
  - Slot 2: 10:15 AM - 11:45 AM
  - Slot 3: 12:00 PM - 1:30 PM
  - Slot 4: 1:45 PM - 3:15 PM
  - Slot 5: 3:45 PM - 5:15 PM
- Each TA has a weekly schedule (e.g., "Ahmed teaches Cloud Computing Lab on Saturday Slot 1")
- Day-off configuration (Friday mandatory + 1-2 extra days per TA)

### 6. Proctor Assignment
- **Auto-assign algorithm** (greedy approach):
  - Checks 3 mandatory rules before assigning
  - Balances workload across TAs (assigns to TA with fewest assignments first)
  - Can add backup proctors automatically
- **Manual assign**: Admin can manually assign specific TAs
- **3 Mandatory Rules**:
  1. Max sessions per semester (hard limit, configurable per TA)
  2. Max 2 sessions per week
  3. No schedule conflict (TA can't proctor during their teaching slot)

### 7. User Management
- Admin creates TA accounts
- Configure per-TA settings: day off, target workload, max sessions
- View all users with role filtering and search

### 8. Notifications
- In-app notifications for schedule changes and assignments
- Mark as read / Mark all as read

### 9. Dashboard & Reporting
- **Admin view**: System stats (total TAs, upcoming quizzes, unassigned quizzes)
- **TA view**: Personal workload summary (completed sessions vs target)
- **Reports page**: TA completion statistics with date filtering

---

## Seed Data for Demo

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@qps.com | password123 |
| TA | ahmed@qps.com | password123 |
| TA | sara@qps.com | password123 |
| TA | omar@qps.com | password123 |
| TA | fatima@qps.com | password123 |

**Courses**: Cloud Computing, Data Structures, Operating Systems, Database Systems

**Semester**: Fall 2026 (active)

**Sample Quizzes**: 3 upcoming quizzes with different locations and proctor requirements

---

## Design Decisions

1. **SQLite over PostgreSQL**: Started with SQLite for simplicity; can migrate later without code changes
2. **Greedy algorithm for v1**: Simple, fast, and good enough for initial implementation; can upgrade to constraint-satisfaction later
3. **Slot-based schedules**: Fixed time slots simplify conflict detection (just check time overlap)
4. **Result pattern**: All operations return `Result<T>` objects for consistent error handling
5. **Clean Architecture**: Separates concerns, makes testing easier, allows swapping implementations

---

## Current Limitations (Known)

1. No Excel export (admin views data in web app only)
2. Greedy algorithm may not find globally optimal assignments
3. No email notifications (in-app only)
4. No professor role (professors don't proctor in this system)
5. Room size → proctor count mapping needs clarification from supervisor

---

## Questions to Ask Your Professor

### About the Algorithm
1. "Is there an existing batch processing algorithm we should integrate with, or should we implement our own proctor assignment logic?"
2. "The current greedy algorithm assigns TAs one-by-one without looking at the big picture. Should we upgrade to a constraint-satisfaction algorithm that considers all assignments globally?"

### About Room/Proctor Configuration
3. "How is the number of proctors per room determined? Is it based on room capacity (e.g., small room = 1 proctor, large room = 3 proctors), or does the admin manually set it?"
4. "What columns does the university's exam schedule Excel sheet contain? We need to know the exact format for the Excel parser."

### About Excuses System
5. "For the excuses system, should TAs submit excuses through the web app (with approval workflow), or does the admin add them directly?"
6. "When a TA submits an excuse, should the system automatically reassign their upcoming proctoring duties, or just block future assignments?"

### About Integration
7. "Does this system need to integrate with any existing university systems (LMS, HR system, etc.)?"
8. "Should the system send email notifications in addition to in-app notifications?"

### About Reporting
9. "Does the admin need to export proctoring assignments to Excel or generate PDF reports?"
10. "Are there specific reports the university needs (e.g., TA workload summary, semester completion stats)?"

### About Future Development
11. "Should we plan for a mobile-responsive version or a dedicated mobile app for TAs to view their assignments?"
12. "Is there a need for conflict-of-interest detection (e.g., TA shouldn't proctor courses they're teaching)?"
