# Quiz Proctoring System: requirements and where they live

Last updated: 4 November 2026 (version 2).

This file collects every requirement found in the project history (chat requests, supervisor answers,
the design spec, the demo site screenshots and the sample university calendar PDF) and points to the
code that satisfies it. Use it to check the system against what was asked, and to see what is still open.

## Sources

| Code | Source |
|------|--------|
| U | Your requests in the project chats (the original brief and later answers) |
| S | Supervisor answers recorded in `docs/supervisor-questions.md` |
| P | The supervisor's demo site (13 screenshots: dashboard, quizzes, add quiz, TA schedules, proctor summary, reviewing) |
| C | The university quiz calendar sample (`INFORMATICS 2nd SEMESTER QUIZ CALENDAR S26`, PDF) |
| D | `docs/superpowers/specs/2026-09-16-quiz-proctoring-system-design.md` |

## 1. The three mandatory rules

All three are enforced in one place, `src/QPS.Domain/Rules/EligibilityRules.cs`. Manual assignment, automatic
assignment, the candidate list and the re-check after any change all call it, so a rule can't be bypassed by
taking another path. Breaking a rule by hand is only possible with **Override** plus a written reason, and the
assignment is then flagged on the quiz board.

| ID | Requirement | Source | Implementation | Status |
|----|-------------|--------|----------------|--------|
| R1 | A TA proctors at most *x* times per semester | U | `SemesterLimit` rule. *x* = Settings → Sessions per TA per semester (default 20), with an optional personal limit per TA (Proctors → Workload → Adjust) | Done, tested (`RuleTests`) |
| R2 | At most 2 proctoring sessions per week | U | `WeeklyLimit` rule. Weeks are 7-day blocks counted from the semester's first day (the university's Tue–Mon weeks). The limit is configurable in Settings | Done, tested |
| R3 | No clash with the TA's regular schedule | U | `TeachingConflict` rule: a quiz may not overlap any class in the TA's timetable. Overlap is strict, so a Gap quiz (3:15–3:45) does not clash with the 4th or 5th slot | Done, tested |

## 2. Further assignment rules

| ID | Requirement | Source | Implementation | Status |
|----|-------------|--------|----------------|--------|
| A1 | Friday is a day off for everyone | S | `FridayOff` rule; Fridays are disabled in every date picker and timetable | Done |
| A2 | Each TA has 1–2 extra days off | S | `DayOff` rule; set per TA (max 3) in the person form | Done |
| A3 | Excused TAs are not assigned (sick, injured, away) with a reason and a period | S, U | `Excused` rule; TAs submit excuses, admins can record them. Saving an excuse lists the TA's sessions inside the period with a **Replace** button | Done |
| A4 | A TA can't be in two places at once | D | `DoubleBooked` and `AlreadyInQuiz` rules | Done |
| A5 | Deactivated accounts never proctor | D | `Inactive` rule; such TAs are left out of the candidate list and can't be forced | Done |
| A6 | Professors don't proctor | S | Only the TA role can be assigned | Done |
| A7 | The system assigns automatically, the admin can override | U (chosen option), P (Auto-assign toggle) | `AssignmentEngine`: a TA of the course goes into each room first (one per room), then seats go to whoever is furthest from their target workload. It only fills empty seats and never moves existing ones | Done, tested |
| A8 | Workload: one session is one point, a soft target and a hard maximum | S | Target (default 14) ranks TAs; the semester limit is hard. Both per TA | Done |
| A9 | Course TAs are flagged | P (flag icon) | Blue chip with a flag on the board, in the candidate list and in history | Done |
| A10 | Reserve proctors ("Add automatic backup") | P | A "Reserve" location with N standby proctors (Settings → Reserve proctors) | Done |

## 3. Quizzes and the university calendar

| ID | Requirement | Source | Implementation | Status |
|----|-------------|--------|----------------|--------|
| Q1 | Read the calendar the university publishes as Excel | U | Import calendar page → `ExcelParserService`. Merged WEEK/DATE/SUBJECT/SLOT cells are read like the printed sheet; holiday banner rows are skipped | Done, tested |
| Q2 | The calendar can also be a PDF | U, C | Read in the browser with pdf.js (`client/src/lib/pdfCalendar.ts`), rebuilding merged cells from the drawn borders; all 31 rows of the sample PDF come out right | Done |
| Q3 | Columns: Week, Date ("Tuesday, April 21, 2026"), Subject, Slot (1st–5th or Gap), GP (GPI–GPIII), Location ("C.101-C.102-C.201") | C, U | Locations are split into one room each; GP is kept as the room's group | Done |
| Q4 | The file has no proctor counts | U | Each room gets its default count (Settings → Rooms), editable in the preview and later | Done |
| Q5 | Keep the week number | U | The file's label is stored and shown; the weekly rule uses the date. A warning appears when the two differ by 2 or more weeks | Done |
| Q6 | Slot times: 8:30–10:00, 10:15–11:45, 12:00–13:30, 13:45–15:15, Gap 15:15–15:45, 15:45–17:15 | U, C | `SlotCatalog` (server) and `SLOTS` (client) | Done |
| Q7 | Manual quiz creation as in the demo (course search, calendar, times, rooms with 2–5 proctors, auto-assign, backup) | P, S | Add quiz page; adds slot quick picks, Midterm/Final type, groups, notes, a live view of other quizzes that day and a warning when a room is already used at that time | Done |
| Q8 | Quiz status updates by itself (Upcoming → Started → Finished) | S, P | Worked out from date and time in the Cairo time zone | Done |
| Q9 | Quiz board grouped by day with filters (search, All/Upcoming/Started/Finished, Incomplete only), staffing per room, duplicate, actions | P | Quizzes page | Done |
| Q10 | Importing twice doesn't create duplicates | D | Commit matches course + date + time and merges rooms | Done, tested |

## 4. People and schedules

| ID | Requirement | Source | Implementation | Status |
|----|-------------|--------|----------------|--------|
| T1 | Weekly teaching timetable per TA (day × slot, course, Lab/Tut/Lecture, room) | D, P | TA schedules page (admin) and My schedule (TA) | Done |
| T2 | TAs edit their own timetable; admins can lock editing per TA or for everyone | P ("Allow editing", "Sch_Edit En") | Toggle per TA and a lock-all action; TA changes notify admins and re-check their upcoming sessions | Done |
| T3 | Import timetables from Excel | D | Template download + import with "replace existing" | Done |
| T4 | Proctor summary: list of TAs with filters, admins list, totals, schedule editing, workload with editable target, excuses with Add, assignment history | P | Proctors page | Done |
| T5 | Add, edit, deactivate and delete people | D | Proctors → Add person / Edit / ⋯ menu. Deleting keeps past history and frees upcoming seats | Done |
| T6 | Courses are managed per department by the admin | S, P (Manage Courses) | Manage courses dialog (add, rename, code, delete when unused) | Done |
| T7 | Semesters, one active at a time | D | Settings → Semesters | Done |

## 5. Reviewing, reports and notifications

| ID | Requirement | Source | Implementation | Status |
|----|-------------|--------|----------------|--------|
| V1 | Dashboard: active TAs, upcoming quizzes, quizzes missing proctors, upcoming schedule, most active TAs, notifications, Manage courses | P | Overview page | Done |
| V2 | Reviewing → TAs: points vs target with edit, history per TA, sort by most/least/target reached/name | P | Reviewing page, TAs tab | Done |
| V3 | Reviewing → Courses: quizzes total/past/upcoming, course TAs, quiz list, sorts | P | Courses tab | Done |
| V4 | Reviewing → Analytics: totals, average sessions per TA, sorted dot chart with average line, ranking | P | Analytics tab | Done |
| V5 | In-app notifications with mark as read / mark all as read | S, P | Bell in the top bar; links open the right page | Done |
| V6 | Export to Excel | U (open question, kept optional) | Quiz board export and workload export | Done (optional) |

## 6. Technical requirements

| ID | Requirement | Source | Implementation |
|----|-------------|--------|----------------|
| X1 | ASP.NET Core on .NET 8 + React | U | .NET 8 Clean Architecture (Domain, Application, Infrastructure, Data, API), React 19 + Vite |
| X2 | Follow the template's patterns: CQRS with MediatR, FluentValidation, EF Core SQLite, JWT, BCrypt | U (template README) | As listed |
| X3 | SQLite | S | `quizproctoring.db`, created and seeded on first run |
| X4 | API on localhost:3000, client on localhost:3001 | U | `launchSettings.json` and `vite.config.ts` (the client proxies `/api`) |
| X5 | Email/password sign in with roles Admin and TA | S | JWT (8 h), role checks on every endpoint |
| X6 | A distinctive interface, not a generic dashboard | U | See "Design" in the README |

## 7. Beyond the demo site

- **Candidate picker with reasons.** Every TA is shown with their day as a time rail (classes and the quiz window), weekly and semester counts and, if unavailable, exactly which rule blocks them.
- **Override with a reason.** Rules can be broken only on purpose; the reason is stored and the chip is flagged.
- **Assignment run.** Plan a date range, see every proposed assignment and every seat nobody can take (with the reason), untick, then confirm.
- **Re-checking.** When a timetable, excuse or quiz time changes, affected assignments are flagged on the board and counted on the overview.
- **Replace.** One click replaces a proctor with the best available TA.
- **TA portal.** Home with the next session, counters against the limits, all sessions with co-proctors, excuses, timetable, password change.
- **PDF import** of the calendar in addition to Excel, with an editable preview before anything is saved.

## 8. Open questions and assumptions

These are also in `docs/supervisor-questions.md`.

1. **The existing batch processing algorithm.** Earlier the supervisor said not to build an algorithm because one exists; later you chose automatic assignment with admin override, which also matches the demo's "Auto-assign Proctors" toggle. The engine is isolated in `src/QPS.Domain/Rules/AssignmentEngine.cs`, so it can be replaced by the university's algorithm without touching the rules, the screens or the API.
2. **Room size → number of proctors.** Rooms store their seat count and a default number of proctors, set by the admin. If the university has a formula (for example one proctor per 30 students), it can fill that default automatically.
3. **Week numbers.** Weeks are counted from the semester start date set in Settings. Holidays can make the official label differ; the label from the file is kept for display.
4. **Points.** Every session counts one point. If finals or longer exams should count more, only the point value per assignment changes.
