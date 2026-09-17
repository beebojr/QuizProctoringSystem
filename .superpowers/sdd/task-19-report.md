# Task 19: Schedule & Assignment Pages

## Status: DONE

## Summary
Created Schedule and Assignment pages for the Quiz Proctoring System.

## Changes Made

### Files Created
- `client/src/pages/Schedules.tsx` - TA schedule management with filtering, CRUD operations, and create modal
- `client/src/pages/Assignments.tsx` - Assignment listing for admins (all) and TAs (own assignments)

### Files Modified
- `client/src/App.tsx:8-9,30-31` - Added imports and routes for Schedules and Assignments pages

## Build Status: PASS
- TypeScript compilation: ✓
- Vite build: ✓ (89 modules, 347.70 kB)

## Commits
- `d82d5e4` - feat: add schedule and assignment pages

## Implementation Notes
- Schedules page includes day-of-week filtering, slot creation modal with TA/semester selection
- Assignments page shows role-based views (Admin sees all, TAs see own assignments)
- Both pages use the existing API layer and auth context
