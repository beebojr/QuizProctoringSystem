# Task 2: Domain Entities & Enums - Report

## Status: DONE

## Summary

Created all domain entities and enums in the QPS.Domain project following Clean Architecture principles.

## Files Created

### Enums (5 files)
- `src/QPS.Domain/Enums/UserRole.cs` - Flags enum for Admin/TA roles
- `src/QPS.Domain/Enums/DayOfWeek.cs` - Custom day of week enum (Friday=0 to Thursday=6)
- `src/QPS.Domain/Enums/QuizStatus.cs` - Quiz lifecycle (Upcoming, Started, Finished)
- `src/QPS.Domain/Enums/AssignmentStatus.cs` - Proctor assignment status (Assigned, Confirmed, Cancelled)
- `src/QPS.Domain/Enums/SlotType.cs` - Schedule slot type (Lab, Tut)

### Entities (11 files)
- `src/QPS.Domain/Entities/BaseEntity.cs` - Abstract base with Id, audit fields, soft delete
- `src/QPS.Domain/Entities/User.cs` - User entity with role, workload, and preferences
- `src/QPS.Domain/Entities/Course.cs` - Course entity with name and department
- `src/QPS.Domain/Entities/Semester.cs` - Semester entity with date range
- `src/QPS.Domain/Entities/Quiz.cs` - Quiz entity with course, semester, locations
- `src/QPS.Domain/Entities/QuizLocation.cs` - Quiz location with proctors needed
- `src/QPS.Domain/Entities/ProctorAssignment.cs` - Assignment junction with status
- `src/QPS.Domain/Entities/TAScheduleSlot.cs` - TA schedule with time slots
- `src/QPS.Domain/Entities/Notification.cs` - User notifications
- `src/QPS.Domain/Entities/Excuse.cs` - TA excuse entries with date range
- `src/QPS.Domain/Entities/WorkloadConfig.cs` - System configuration

## Files Deleted
- `src/QPS.Domain/Class1.cs` - Removed default placeholder

## Build Status: PASS

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Commits

| SHA | Message |
|-----|---------|
| 4e9db67 | feat(domain): add domain entities and enums |

## Issues Resolved

- Fixed `CS0104: 'DayOfWeek' is an ambiguous reference` error by fully qualifying `Enums.DayOfWeek` in User.cs and TAScheduleSlot.cs to avoid conflict with `System.DayOfWeek`.

## Notes

- Domain layer has zero external dependencies as required
- All entities inherit from BaseEntity for consistent audit fields
- Navigation properties set up for future EF Core relationships
- Nullable reference types enabled throughout
