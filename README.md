# Quiz Proctoring System

Assigns teaching assistants to quiz, midterm and final proctoring based on when they are free. The
university quiz calendar (Excel or PDF) is imported, every room gets its proctors, and three rules are
never broken without a written reason:

1. A TA proctors at most *x* sessions per semester.
2. A TA proctors at most 2 sessions per academic week.
3. A proctoring session never overlaps the TA's teaching schedule.

Fridays, personal days off and excuses are respected too. Full traceability is in
[`docs/REQUIREMENTS.md`](docs/REQUIREMENTS.md).

## Run it

You need the .NET 8 SDK and Node.js 20 or newer.

```powershell
# 1. API on http://localhost:3000 (creates and fills quizproctoring.db on first run)
dotnet run --project src/QPS.API

# 2. Web app on http://localhost:3001 (second terminal)
cd client
npm install
npm run dev
```

Open http://localhost:3001 and sign in. Every demo account uses the password `password123`.

| Account | Role |
|---------|------|
| `admin@qps.com` | Administrator |
| `ahmed@qps.com`, `sara@qps.com`, `omar@qps.com` and more | Teaching assistants |

To start again from fresh demo data, stop the API and delete `src/QPS.API/quizproctoring.db*`.
Turn demo data off with `"App": { "SeedDemoData": false }` in `src/QPS.API/appsettings.json`.

Rule tests: `dotnet run --project tests/QPS.Tests` (18 tests: the three rules, days off, excuses, the
engine, calendar parsing).

API documentation (Swagger) is at http://localhost:3000/swagger while the API runs in Development.

## What's where

```
src/
  QPS.Domain/           Entities, enums, Scheduling (slots, academic weeks) and Rules
    Rules/EligibilityRules.cs   the single place where every assignment rule lives
    Rules/AssignmentEngine.cs   automatic assignment (course TAs first, then by fairness)
  QPS.Application/      CQRS features (MediatR handlers, FluentValidation), DTOs, interfaces
  QPS.Infrastructure/   JWT, current user, notifications, Excel read/write (ClosedXML), clock
  QPS.Data/             EF Core DbContext, migration, seed data
  QPS.API/              Controllers, error handling, JSON settings, Program.cs
tests/QPS.Tests/        Rule and parser tests (plain console runner, no extra packages)
client/                 React 19 + Vite + TypeScript
  src/pages/admin       Overview, Quizzes, Add/Edit quiz, Import calendar, Assignment run,
                        TA schedules, Proctors, Reviewing, Settings
  src/pages/ta          Home, My proctoring, My schedule, Excuses
  src/components        Quiz card with seat strips, candidate picker, slot rail, timetable grid
  src/lib/pdfCalendar.ts  reads the calendar PDF in the browser
docs/                   Requirements, supervisor questions, presentation notes, earlier spec and plans
```

## How assignment works

- **By hand:** click a dashed seat on a quiz. Every TA is listed with their day drawn as a rail (classes in
  ink, the quiz time highlighted) and their weekly and semester counts. Unavailable TAs show the rule that
  blocks them; **Override** needs a reason, which is stored and flagged.
- **Automatically:** "Assign proctors automatically" on a new quiz, "Fill empty seats" on a quiz card,
  "Assign proctors now" when importing, or the **Assignment run** page for a whole date range (preview
  first, then confirm). The engine places one TA of the course in each room, then gives the remaining
  seats to whoever is furthest from their target. It only fills empty seats.
- **After changes:** if a timetable, excuse or quiz time changes, affected assignments are flagged on the
  board and counted on the overview, and **Replace** picks the best available TA.

## Design

The interface is built like an exam office's paperwork rather than a generic dashboard: cool paper,
blue-black ink, and one highlighter yellow that always means "the time in question" (the current week in
the side bar, today, the quiz window on a TA's day). Each room on a quiz card is a strip of seats; empty
seats are dashed and clickable. Typefaces: Archivo (expanded) for headings and room codes, IBM Plex Sans
for text, both bundled with the app so it works offline.

## Notes

- The previous database `src/QPS.API/qps.db` is no longer used and can be deleted.
- Files from the first version that are no longer used were emptied (they only contain a comment).
  `cleanup-old-files.ps1` deletes them; it's optional.
- Change `Jwt:Secret` in `appsettings.json` before deploying anywhere.
