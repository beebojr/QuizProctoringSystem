# Questions for Supervisor Review

These questions were raised during the design phase of the Quiz Proctoring System. Please review with your supervisor and return with answers so we can finalize the design.

---

## ANSWERED (During Design Sessions)

### Authentication Method (CONFIRMED)
**Decision:** Email/password authentication with BCrypt hashing.

### Database (CONFIRMED)
**Decision:** Start with SQLite, migrate to PostgreSQL later if needed.

### Slot Times (CONFIRMED)
- Slot 1: 8:30 AM - 10:00 AM
- Slot 2: 10:15 AM - 11:45 AM
- Slot 3: 12:00 PM - 1:30 PM
- Slot 4: 1:45 PM - 3:15 PM
- Slot 5: 3:45 PM - 5:15 PM

Each slot is 90 minutes with 15-minute breaks.

### TA Days Off (CONFIRMED)
- Friday is mandatory off for ALL TAs
- Each TA has 1-2 additional days off per week (configured per TA)
- TAs typically have 2 days off per week total

### Workload System (CONFIRMED)
- Each proctoring assignment = 1 point
- Each TA has a "Target Workload" (goal) and "Current Workload" (actual)
- Both the target (soft limit) and max sessions (hard limit) exist

### Quiz Creation (CONFIRMED)
- Both Excel import AND manual quiz creation supported
- Admin can add quizzes via form (like demo) or import from Excel

### Course Management (CONFIRMED)
- Courses are pre-defined per department
- Admin can add new courses occasionally (rare curriculum changes)
- Courses are a separate entity

### Quiz Status (CONFIRMED)
- Auto-changes based on date/time (Upcoming → Started → Finished)
- Admin can manually override if auto-update fails

### Notifications (CONFIRMED)
- In-app notifications for schedule changes and assignments
- "Mark all read" functionality

---

## PENDING (Need Supervisor Answers)

### 1. Excel Exam Schedule Format (HIGH PRIORITY)

**Question:** What columns does the university's exam schedule Excel sheet contain?

**Why it matters:** We need to know the exact column layout to build the Excel parser.

**Current assumption:** Fixed format with columns: `Exam Name, Course, Date, Start Time, End Time, Room, Proctors Needed`. If "Proctors Needed" is missing, the system defaults to 1.

---

### 2. Number of Proctors Per Exam (HIGH PRIORITY)

**Question:** How is the number of proctors needed for each exam determined?

**Options:**
- Admin manually specifies per exam after import
- Included in the Excel schedule
- Derived from room capacity or student count

**Current assumption:** Excel includes a "Proctors Needed" column; admin can override after import.

---

### 3. Excuses System (MEDIUM PRIORITY)

**Question:** How does the 'Excuses' system work? Can TAs request excuses, or does admin add them directly?

**Why it matters:** This affects whether we need a request/approval workflow or just a simple CRUD operation.

---

### 4. Professor Proctoring Role (LOW PRIORITY)

**Question:** When professors do proctor (rare cases), do they follow the same rules as TAs (max sessions, weekly limits), or are they exempt?

**Current assumption:** Professors are optional in the system. If added, they follow the same rules as TAs.

---

### 5. Report/Export Requirements (LOW PRIORITY)

**Question:** Does the admin need to export proctoring assignments to Excel or generate reports?

**Current assumption:** No export in the initial version; admin views assignments in the web app only.

---

*Please bring these questions to your supervisor. Once answered, we can finalize the design and proceed to implementation planning.*
