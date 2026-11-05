# Optional: deletes files from the first version that are no longer used.
# They were emptied (comment only), so the project builds with or without this script.
# Run from the project folder:  powershell -ExecutionPolicy Bypass -File .\cleanup-old-files.ps1

$files = @(
  "src/QPS.Application/Common/Interfaces/IProctorAssignmentService.cs",
  "src/QPS.Application/Features/Assignments/Commands/AutoAssign.cs",
  "src/QPS.Application/Features/Assignments/Commands/ManualAssign.cs",
  "src/QPS.Application/Features/Assignments/Queries/GetAssignments.cs",
  "src/QPS.Application/Features/Auth/Commands/Register.cs",
  "src/QPS.Application/Features/Courses/Commands/CreateCourse.cs",
  "src/QPS.Application/Features/Courses/Commands/DeleteCourse.cs",
  "src/QPS.Application/Features/Dashboard/DTOs/DashboardDtos.cs",
  "src/QPS.Application/Features/Dashboard/Queries/GetReport.cs",
  "src/QPS.Application/Features/Dashboard/Queries/GetSystemDashboard.cs",
  "src/QPS.Application/Features/Dashboard/Queries/GetWorkloadSummary.cs",
  "src/QPS.Application/Features/Notifications/Commands/MarkAllAsRead.cs",
  "src/QPS.Application/Features/Notifications/Commands/MarkAsRead.cs",
  "src/QPS.Application/Features/Notifications/DTOs/NotificationDtos.cs",
  "src/QPS.Application/Features/Notifications/Queries/GetNotifications.cs",
  "src/QPS.Application/Features/Quizzes/Commands/CreateQuiz.cs",
  "src/QPS.Application/Features/Quizzes/Commands/ImportQuizzes.cs",
  "src/QPS.Application/Features/Schedules/Commands/CreateScheduleSlot.cs",
  "src/QPS.Application/Features/Schedules/Commands/DeleteScheduleSlot.cs",
  "src/QPS.Application/Features/Semesters/Commands/CreateSemester.cs",
  "src/QPS.Application/Features/Semesters/Commands/UpdateSemester.cs",
  "src/QPS.Application/Features/Users/Commands/CreateUser.cs",
  "src/QPS.Domain/Entities/WorkloadConfig.cs",
  "src/QPS.Domain/Enums/AssignmentStatus.cs",
  "src/QPS.Infrastructure/Services/ProctorAssignmentService.cs",
  "src/QPS.Data/Migrations/20260924085110_InitialCreate.cs",
  "src/QPS.Data/Migrations/20260924085110_InitialCreate.Designer.cs",
  "src/QPS.Data/Migrations/20260924141837_AddQuizSlotAndGroup.cs",
  "src/QPS.Data/Migrations/20260924141837_AddQuizSlotAndGroup.Designer.cs",
  "src/QPS.Data/Migrations/20260924142717_AddQuizWeekNumber.cs",
  "src/QPS.Data/Migrations/20260924142717_AddQuizWeekNumber.Designer.cs",
  "client/src/api/axios.ts",
  "client/src/components/Layout.tsx",
  "client/src/pages/Assignments.tsx",
  "client/src/pages/Dashboard.tsx",
  "client/src/pages/Quizzes.tsx",
  "client/src/pages/Reports.tsx",
  "client/src/pages/Schedules.tsx",
  "client/src/pages/Users.tsx",
  "client/src/index.css",
  "client/src/assets/hero.png",
  "client/src/assets/vite.svg",
  "client/public/icons.svg"
)

foreach ($f in $files) {
  $path = Join-Path $PSScriptRoot $f
  if (Test-Path $path) { Remove-Item $path; Write-Host "deleted $f" }
}
foreach ($d in @("client/src/hooks", "client/src/types", "client/src/assets")) {
  $path = Join-Path $PSScriptRoot $d
  if ((Test-Path $path) -and -not (Get-ChildItem $path)) { Remove-Item $path; Write-Host "removed empty folder $d" }
}
Write-Host "Done. The old database src/QPS.API/qps.db is not used any more; delete it yourself if you don't need it."
