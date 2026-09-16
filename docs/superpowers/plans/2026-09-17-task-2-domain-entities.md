# Task 2: Domain Entities & Enums Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create all domain entities and enums in the QPS.Domain project following Clean Architecture principles.

**Architecture:** Domain layer with zero dependencies on other layers. Uses base entity class with audit fields, clean enum definitions, and proper navigation properties.

**Tech Stack:** .NET 8.0, C# 12, nullable reference types enabled

**Spec:** Task 2 specifications (provided inline)

## Global Constraints

- Target framework: net8.0
- Nullable reference types: enabled
- Implicit usings: enabled
- Domain layer has zero external dependencies

## File Structure

### Enums (5 files)
- `src/QPS.Domain/Enums/UserRole.cs` - Flags enum for user roles
- `src/QPS.Domain/Enums/DayOfWeek.cs` - Custom day of week enum
- `src/QPS.Domain/Enums/QuizStatus.cs` - Quiz lifecycle status
- `src/QPS.Domain/Enums/AssignmentStatus.cs` - Proctor assignment status
- `src/QPS.Domain/Enums/SlotType.cs` - Schedule slot type

### Entities (11 files)
- `src/QPS.Domain/Entities/BaseEntity.cs` - Abstract base with audit fields
- `src/QPS.Domain/Entities/User.cs` - User entity with role and preferences
- `src/QPS.Domain/Entities/Course.cs` - Course entity
- `src/QPS.Domain/Entities/Semester.cs` - Semester entity
- `src/QPS.Domain/Entities/Quiz.cs` - Quiz with locations
- `src/QPS.Domain/Entities/QuizLocation.cs` - Quiz location with assignments
- `src/QPS.Domain/Entities/ProctorAssignment.cs` - Assignment junction entity
- `src/QPS.Domain/Entities/TAScheduleSlot.cs` - TA schedule entry
- `src/QPS.Domain/Entities/Notification.cs` - User notifications
- `src/QPS.Domain/Entities/Excuse.cs` - TA excuse entries
- `src/QPS.Domain/Entities/WorkloadConfig.cs` - System configuration

---

## Task 1: Remove Default Class1.cs

**Files:**
- Delete: `src/QPS.Domain/Class1.cs`

**Steps:**

- [ ] **Step 1: Delete Class1.cs**

```bash
rm src/QPS.Domain/Class1.cs
```

- [ ] **Step 2: Verify deletion**

```bash
ls src/QPS.Domain/
```

Expected: Class1.cs is no longer present

---

## Task 2: Create Enum Files

**Files:**
- Create: `src/QPS.Domain/Enums/UserRole.cs`
- Create: `src/QPS.Domain/Enums/DayOfWeek.cs`
- Create: `src/QPS.Domain/Enums/QuizStatus.cs`
- Create: `src/QPS.Domain/Enums/AssignmentStatus.cs`
- Create: `src/QPS.Domain/Enums/SlotType.cs`

**Steps:**

- [ ] **Step 1: Create Enums directory**

```bash
mkdir src/QPS.Domain/Enums
```

- [ ] **Step 2: Create UserRole.cs**

```csharp
namespace QPS.Domain.Enums;

[Flags]
public enum UserRole
{
    Admin = 1,
    TA = 2
}
```

- [ ] **Step 3: Create DayOfWeek.cs**

```csharp
namespace QPS.Domain.Enums;

public enum DayOfWeek
{
    Friday = 0,
    Saturday = 1,
    Sunday = 2,
    Monday = 3,
    Tuesday = 4,
    Wednesday = 5,
    Thursday = 6
}
```

- [ ] **Step 4: Create QuizStatus.cs**

```csharp
namespace QPS.Domain.Enums;

public enum QuizStatus
{
    Upcoming = 0,
    Started = 1,
    Finished = 2
}
```

- [ ] **Step 5: Create AssignmentStatus.cs**

```csharp
namespace QPS.Domain.Enums;

public enum AssignmentStatus
{
    Assigned = 0,
    Confirmed = 1,
    Cancelled = 2
}
```

- [ ] **Step 6: Create SlotType.cs**

```csharp
namespace QPS.Domain.Enums;

public enum SlotType
{
    Lab = 0,
    Tut = 1
}
```

---

## Task 3: Create Base Entity

**Files:**
- Create: `src/QPS.Domain/Entities/BaseEntity.cs`

**Steps:**

- [ ] **Step 1: Create Entities directory**

```bash
mkdir src/QPS.Domain/Entities
```

- [ ] **Step 2: Create BaseEntity.cs**

```csharp
namespace QPS.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
```

---

## Task 4: Create User Entity

**Files:**
- Create: `src/QPS.Domain/Entities/User.cs`

**Steps:**

- [ ] **Step 1: Create User.cs**

```csharp
using QPS.Domain.Enums;

namespace QPS.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DayOfWeek? DayOff { get; set; }
    public int TargetWorkload { get; set; } = 14;
    public int MaxProctoringSessionsPerSemester { get; set; } = 10;
    public bool IsActive { get; set; } = true;
    
    public string FullName => $"{FirstName} {LastName}";
}
```

---

## Task 5: Create Course and Semester Entities

**Files:**
- Create: `src/QPS.Domain/Entities/Course.cs`
- Create: `src/QPS.Domain/Entities/Semester.cs`

**Steps:**

- [ ] **Step 1: Create Course.cs**

```csharp
namespace QPS.Domain.Entities;

public class Course : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Create Semester.cs**

```csharp
namespace QPS.Domain.Entities;

public class Semester : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; }
}
```

---

## Task 6: Create Quiz and QuizLocation Entities

**Files:**
- Create: `src/QPS.Domain/Entities/Quiz.cs`
- Create: `src/QPS.Domain/Entities/QuizLocation.cs`

**Steps:**

- [ ] **Step 1: Create Quiz.cs**

```csharp
using QPS.Domain.Enums;

namespace QPS.Domain.Entities;

public class Quiz : BaseEntity
{
    public Guid CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public Guid SemesterId { get; set; }
    public Semester Semester { get; set; } = null!;
    public DateOnly QuizDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public QuizStatus Status { get; set; }
    public bool AutoAssign { get; set; }
    public bool AddBackup { get; set; }
    
    public ICollection<QuizLocation> Locations { get; set; } = new List<QuizLocation>();
}
```

- [ ] **Step 2: Create QuizLocation.cs**

```csharp
namespace QPS.Domain.Entities;

public class QuizLocation : BaseEntity
{
    public Guid QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;
    public string RoomName { get; set; } = string.Empty;
    public int ProctorsNeeded { get; set; }
    
    public ICollection<ProctorAssignment> Assignments { get; set; } = new List<ProctorAssignment>();
}
```

---

## Task 7: Create ProctorAssignment and TAScheduleSlot Entities

**Files:**
- Create: `src/QPS.Domain/Entities/ProctorAssignment.cs`
- Create: `src/QPS.Domain/Entities/TAScheduleSlot.cs`

**Steps:**

- [ ] **Step 1: Create ProctorAssignment.cs**

```csharp
using QPS.Domain.Enums;

namespace QPS.Domain.Entities;

public class ProctorAssignment : BaseEntity
{
    public Guid QuizLocationId { get; set; }
    public QuizLocation QuizLocation { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public bool IsBackup { get; set; }
    public AssignmentStatus Status { get; set; }
}
```

- [ ] **Step 2: Create TAScheduleSlot.cs**

```csharp
using QPS.Domain.Enums;

namespace QPS.Domain.Entities;

public class TAScheduleSlot : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid SemesterId { get; set; }
    public Semester Semester { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; }
    public int SlotNumber { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public SlotType SlotType { get; set; }
    public string Room { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
}
```

---

## Task 8: Create Notification, Excuse, and WorkloadConfig Entities

**Files:**
- Create: `src/QPS.Domain/Entities/Notification.cs`
- Create: `src/QPS.Domain/Entities/Excuse.cs`
- Create: `src/QPS.Domain/Entities/WorkloadConfig.cs`

**Steps:**

- [ ] **Step 1: Create Notification.cs**

```csharp
namespace QPS.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
}
```

- [ ] **Step 2: Create Excuse.cs**

```csharp
namespace QPS.Domain.Entities;

public class Excuse : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Reason { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; }
}
```

- [ ] **Step 3: Create WorkloadConfig.cs**

```csharp
namespace QPS.Domain.Entities;

public class WorkloadConfig : BaseEntity
{
    public int TargetWorkload { get; set; } = 14;
}
```

---

## Task 9: Build and Verify

**Files:**
- Build: `src/QPS.Domain/QPS.Domain.csproj`

**Steps:**

- [ ] **Step 1: Build the project**

```bash
dotnet build src/QPS.Domain/QPS.Domain.csproj
```

Expected: Build succeeds with no errors

- [ ] **Step 2: Verify all files created**

```bash
ls src/QPS.Domain/Enums/
ls src/QPS.Domain/Entities/
```

Expected: All 16 files present (5 enums + 11 entities)

---

## Task 10: Commit Changes

**Files:**
- Commit: All created files

**Steps:**

- [ ] **Step 1: Stage changes**

```bash
git add src/QPS.Domain/
```

- [ ] **Step 2: Commit**

```bash
git commit -m "feat(domain): add domain entities and enums

- Add enums: UserRole, DayOfWeek, QuizStatus, AssignmentStatus, SlotType
- Add entities: BaseEntity, User, Course, Semester, Quiz, QuizLocation,
  ProctorAssignment, TAScheduleSlot, Notification, Excuse, WorkloadConfig
- Remove default Class1.cs placeholder"
```

- [ ] **Step 3: Verify commit**

```bash
git log --oneline -1
git status
```

Expected: Clean working tree, commit shows correct message
