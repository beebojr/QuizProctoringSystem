# Quiz Proctoring System — Supervisor Questions (v2)

Last updated: 2026-09-17

---

## CONFIRMED ANSWERS

| # | Question | Answer |
|---|----------|--------|
| 1 | Database | SQLite (migrate to PostgreSQL later if needed) |
| 2 | Authentication | Email/password with BCrypt hashing |
| 3 | Slot times | 5 fixed 90-min slots (8:30-10:00, 10:15-11:45, 12:00-13:30, 13:45-15:15, 15:45-17:15) |
| 4 | TA days off | Friday mandatory + 1-2 extra days per TA |
| 5 | Workload | Each assignment = 1 point; target (soft) and max (hard) limits |
| 6 | Quiz creation | Both Excel import AND manual creation |
| 7 | Course management | Pre-defined per department, admin-managed |
| 8 | Quiz status | Auto-updates based on date/time (Upcoming → Started → Finished) |
| 9 | Notifications | In-app notifications with mark as read |
| 10 | Proctors per exam | **Room size determines proctor count** (details TBD) |
| 11 | Excuses system | TAs submit excuse with reason + duration → system blocks assignments during unavailable period |
| 12 | Professor role | Professors do not proctor |

---

## PENDING QUESTIONS

### 1. Existing Batch Processing Algorithm (HIGH PRIORITY)

**Question:** You mentioned there is an existing batch processing algorithm. What is it?

**What we need to know:**
- Is it an external API we call?
- A separate system we integrate with?
- Or does it mean we should just store assignments without auto-assignment logic?

**Why it matters:** This determines whether we keep, replace, or remove the proctor assignment algorithm in our system.

---

### 2. Excel Exam Schedule Format (HIGH PRIORITY)

**Question:** What columns does the university's exam schedule Excel sheet contain?

**Answer:** The format has been confirmed from the provided PDF:

| Column | Content | Example |
|--------|---------|---------|
| WEEK | Week number (reference only) | Week 7, Week 8 |
| DATE | Full date with day name | Tuesday, April 21, 2026 |
| SUBJECT | Course name | Computer Networks |
| SLOT | Time slot | 1ST, 2ND, 3RD, 4TH, 5TH, or Gap |
| GP | Group number | GPI, GPII, GPIII |
| LOCATION | Room(s) separated by hyphens | M1.105-M1.205 |

**Slot Time Mapping:**
- 1ST: 8:30 AM - 10:00 AM
- 2ND: 10:15 AM - 11:45 AM
- 3RD: 12:00 PM - 1:30 PM
- 4TH: 1:45 PM - 3:15 PM
- Gap: 3:15 PM - 3:45 PM
- 5TH: 3:45 PM - 5:15 PM

**Status:** ✅ CONFIRMED — System updated to handle this format.

---

### 3. Room Size → Proctor Count (MEDIUM PRIORITY)

**Question:** How does room size map to number of proctors?

**Answer:** The number of proctors is dynamic and depends on:
- The proctor assignment algorithm
- TAs with active excuses (unavailable)
- Hall/room size

**Status:** ⚠️ PARTIALLY ANSWERED — Need specific rules for how room size affects proctor count. Currently defaults to 1 proctor per room, admin can adjust manually.

---

### 4. Report/Export Requirements (LOW PRIORITY)

**Question:** Does the admin need to export proctoring assignments to Excel or generate reports?

**Current assumption:** No export in the initial version; admin views assignments in the web app only.

---

*Please bring these questions to your professor. Once answered, we can finalize the design and update the system accordingly.*
