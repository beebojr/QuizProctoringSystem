# Quiz Proctoring System: questions for the supervisor (v3)

Last updated: 4 November 2026

## Confirmed

| # | Topic | Answer | In the system |
|---|-------|--------|---------------|
| 1 | Database | SQLite | `quizproctoring.db` |
| 2 | Sign in | Email and password | BCrypt + JWT, roles Admin and TA |
| 3 | Slots | 1st 8:30–10:00, 2nd 10:15–11:45, 3rd 12:00–13:30, 4th 13:45–15:15, Gap 15:15–15:45, 5th 15:45–17:15 | Fixed catalogue |
| 4 | Days off | Friday for everyone plus 1–2 days per TA | Rules `FridayOff`, `DayOff` |
| 5 | Workload | One point per session; a target and a hard maximum | Settings + per-TA overrides |
| 6 | Creating quizzes | Calendar import (Excel or PDF) and manual entry | Import calendar, Add quiz |
| 7 | Courses | Managed by the admin per department | Manage courses |
| 8 | Quiz status | Updates by itself from date and time | Upcoming, Started, Finished |
| 9 | Notifications | In-app, with mark as read | Bell in the top bar |
| 10 | Excuses | TA gives a reason and a period; no assignments in it | Excuses page and rule |
| 11 | Professors | Don't proctor | Only TAs are assigned |
| 12 | Calendar format | WEEK, DATE, SUBJECT, SLOT, GP, LOCATION; no proctor counts | Parser reads merged cells and splits rooms |
| 13 | Week number | Needed, used as a reference | Stored and shown |

## Still open

### 1. The existing batch processing algorithm (high priority)
You mentioned the university already has a batch algorithm. The demo site has an "Auto-assign Proctors" switch,
so the system currently assigns automatically and lets the admin override.

- Is the existing algorithm something we should call (an API or a program), or a description of how assignment should work?
- If we should call it, what does it take as input and what does it return?

*Why it matters:* the built-in engine (`AssignmentEngine.cs`) can be replaced by it without changing anything else.

### 2. How room size decides the number of proctors (medium)
Rooms store their seat count and a default number of proctors that the admin sets.

- Is there a rule, such as one proctor per 30 students, or a fixed number per hall?
- Do midterms and finals need more proctors than quizzes in the same room?

### 3. Do finals and midterms count more than a quiz? (medium)
Every session counts one point toward a TA's target today.

### 4. Who may override a rule? (low)
Admins can assign a TA who breaks a rule if they write a reason. Should that be allowed, and for which rules?

### 5. Exports (low)
The quiz board and the workload can be exported to Excel. Is another format or report needed (for example a
printable sheet per room for the day of the exam)?
