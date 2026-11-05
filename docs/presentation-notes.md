# Quiz Proctoring System: presentation notes (version 2)

## The problem
Every semester the university publishes a quiz calendar: course, date, slot, student group and rooms.
Each room needs proctors, and the proctors are teaching assistants who also teach. Assigning them by hand
means checking everyone's timetable, days off, excuses and how much they've already done.

## What the system does
1. **Imports the calendar** from Excel or PDF, exactly as the university prints it (merged cells, holiday
   rows, "C.101-C.102" room lists, Gap slot). The admin checks and edits a preview before saving.
2. **Knows when every TA is free** from their weekly teaching timetable, days off and excuses.
3. **Assigns proctors** automatically or by hand, always under the same rules.
4. **Keeps it fair**: TAs who are furthest from their target get seats first; a TA of the course goes in
   each room first.
5. **Keeps everyone informed** with in-app notifications and a TA portal.

## The rules (one file: `src/QPS.Domain/Rules/EligibilityRules.cs`)
- Rule 1: at most *x* sessions per semester (default 20, can differ per TA).
- Rule 2: at most 2 sessions per academic week (weeks counted from the semester start).
- Rule 3: no overlap with the TA's classes.
- Also: Fridays off, personal days off, excuses, no double booking, deactivated accounts never assigned.
- An admin may break a rule only with a written reason; the assignment is flagged.

## Demo script (about 8 minutes)
1. Sign in as `admin@qps.com`. Overview: this week by day, open seats, what needs attention.
2. **Import calendar**: drop the Excel or PDF calendar, show the preview (rooms split, groups, week), import with "Assign proctors now".
3. **Quizzes**: a quiz card, its rooms and seats. Click a dashed seat: the candidate list with each TA's day on a rail and the rule that blocks the others. Assign one.
4. Override a rule with a reason; show the flag on the chip.
5. **Assignment run**: plan the rest of the semester, show seats nobody can take and why, confirm.
6. **Proctors**: record an excuse for a TA; the system lists their sessions in that period and replaces them.
7. **Reviewing → Analytics**: sessions per TA against the average.
8. Sign in as `ahmed@qps.com`: next session, limits, timetable, submit an excuse.

## Architecture
- **.NET 8, Clean Architecture**: Domain (entities, rules, engine) → Application (CQRS with MediatR,
  FluentValidation) → Infrastructure (JWT, Excel, notifications) and Data (EF Core, SQLite) → API.
- **React 19 + Vite + TypeScript** client on port 3001, calling the API on port 3000.
- The rules and the engine are plain C# with no database access, so they are unit tested directly
  (`tests/QPS.Tests`, 18 tests).

## Questions to raise
See `docs/supervisor-questions.md`: the existing batch algorithm, how room size sets the number of
proctors, whether finals count more, who may override a rule, and export needs.
