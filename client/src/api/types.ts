// Shapes returned by the ASP.NET Core API (camelCase JSON, enums as strings).

export type Role = 'Admin' | 'TA'
export type Day = 'Friday' | 'Saturday' | 'Sunday' | 'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday'
export type QuizStatus = 'Upcoming' | 'Started' | 'Finished'
export type ExamType = 'Quiz' | 'Midterm' | 'Final'
export type SlotType = 'Lab' | 'Tut' | 'Lecture'

export interface Result<T> {
  isSuccess: boolean
  value?: T
  message?: string
  errors: string[]
}

export interface CurrentUser {
  id: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  role: Role
  department: string
  canEditSchedule: boolean
  daysOff: Day[]
}

export interface AuthResponse {
  accessToken: string
  expiresAt: string
  user: CurrentUser
}

export interface Semester {
  id: string
  name: string
  startDate: string
  endDate: string
  isActive: boolean
  currentWeek: number
  totalWeeks: number
}

export interface Settings {
  defaultMaxSessionsPerSemester: number
  maxSessionsPerWeek: number
  defaultTargetWorkload: number
  defaultProctorsPerRoom: number
  reserveProctors: number
  preferCourseTas: boolean
  departmentName: string
}

export interface Course {
  id: string
  name: string
  code?: string | null
  department: string
  quizCount: number
  taCount: number
}

export interface Room {
  id: string
  name: string
  capacity?: number | null
  defaultProctors: number
  timesUsed: number
}

export interface User {
  id: string
  email: string
  firstName: string
  lastName: string
  fullName: string
  role: Role
  department: string
  daysOff: Day[]
  maxSessionsOverride?: number | null
  targetWorkloadOverride?: number | null
  effectiveMaxSessions: number
  effectiveTargetWorkload: number
  canEditSchedule: boolean
  isActive: boolean
  lastLoginAt?: string | null
}

export interface ProctorChip {
  assignmentId: string
  userId: string
  name: string
  email: string
  isCourseTa: boolean
  source: 'Auto' | 'Manual'
  overrideReason?: string | null
  warnings: string[]
}

export interface QuizLocation {
  id: string
  roomName: string
  group?: string | null
  proctorsNeeded: number
  isReserve: boolean
  sortOrder: number
  assigned: number
  proctors: ProctorChip[]
}

export interface Quiz {
  id: string
  courseId: string
  courseName: string
  semesterId: string
  examType: ExamType
  weekNumber: number
  quizDate: string
  startTime: string
  endTime: string
  slotNumber?: number | null
  slotLabel: string
  status: QuizStatus
  autoAssign: boolean
  hasReserve: boolean
  notes?: string | null
  source: 'Manual' | 'Import'
  proctorsNeeded: number
  proctorsAssigned: number
  isFullyStaffed: boolean
  warningCount: number
  locations: QuizLocation[]
}

export interface Shortfall {
  quizId: string
  locationId: string
  courseName: string
  date: string
  startTime: string
  endTime: string
  roomName: string
  missing: number
  reasons: string[]
}

export interface AutoAssignSummary {
  assigned: number
  missing: number
  shortfalls: Shortfall[]
}

export interface QuizSaveResult {
  quiz: Quiz
  warnings: string[]
  autoAssign?: AutoAssignSummary | null
}

export interface LocationInput {
  id?: string | null
  roomName: string
  group?: string | null
  proctorsNeeded: number
  isReserve?: boolean
}

export interface Assignment {
  assignmentId: string
  userId: string
  taName: string
  taEmail: string
  quizId: string
  locationId: string
  courseName: string
  examType: ExamType
  date: string
  startTime: string
  endTime: string
  weekNumber: number
  slotLabel: string
  roomName: string
  group?: string | null
  isReserve: boolean
  isCourseTa: boolean
  source: 'Auto' | 'Manual'
  status: QuizStatus
  points: number
  coProctors: string[]
}

export interface RuleViolation {
  code: string
  message: string
}

export interface TeachingBrief {
  slotNumber: number
  startTime: string
  endTime: string
  courseName: string
  type: string
}

export interface Candidate {
  userId: string
  name: string
  email: string
  eligible: boolean
  isCourseTa: boolean
  violations: RuleViolation[]
  sessionsThisSemester: number
  sessionsThisWeek: number
  maxSessions: number
  maxPerWeek: number
  points: number
  targetWorkload: number
  teachingThatDay: TeachingBrief[]
}

export interface Candidates {
  locationId: string
  roomName: string
  isReserve: boolean
  quizId: string
  courseName: string
  date: string
  startTime: string
  endTime: string
  weekNumber: number
  proctorsNeeded: number
  assigned: number
  candidates: Candidate[]
}

export interface AssignOutcome {
  assigned: boolean
  assignmentId?: string | null
  violations: RuleViolation[]
  message: string
}

export interface BatchItem {
  quizId: string
  locationId: string
  userId: string
  courseName: string
  date: string
  startTime: string
  endTime: string
  weekNumber: number
  roomName: string
  isReserve: boolean
  taName: string
  isCourseTa: boolean
}

export interface BatchPreview {
  quizCount: number
  emptySeats: number
  items: BatchItem[]
  shortfalls: Shortfall[]
}

export interface ScheduleSlot {
  id: string
  userId: string
  taName: string
  day: Day
  slotNumber: number
  slotLabel: string
  courseId?: string | null
  courseName: string
  slotType: SlotType
  room: string
  startTime: string
  endTime: string
}

export interface TaScheduleSummary {
  userId: string
  name: string
  email: string
  daysOff: Day[]
  slotCount: number
  canEditSchedule: boolean
  isActive: boolean
}

export interface Excuse {
  id: string
  userId: string
  taName: string
  reason: string
  startDate: string
  endDate: string
  submittedByTa: boolean
  isActive: boolean
  createdAt: string
}

export interface AffectedAssignment {
  assignmentId: string
  quizId: string
  courseName: string
  date: string
  startTime: string
  endTime: string
  roomName: string
}

export interface NotificationItem {
  id: string
  message: string
  type: 'General' | 'Assignment' | 'Schedule' | 'Excuse' | 'Quiz'
  link?: string | null
  isRead: boolean
  createdAt: string
}

export interface NotificationFeed {
  unreadCount: number
  items: NotificationItem[]
}

export interface QuizBrief {
  id: string
  courseName: string
  examType: ExamType
  date: string
  startTime: string
  endTime: string
  weekNumber: number
  slotLabel: string
  rooms: string[]
  needed: number
  assigned: number
  status: QuizStatus
}

export interface TaRank {
  userId: string
  name: string
  assignments: number
  points: number
}

export interface DayLoad {
  date: string
  quizzes: number
  needed: number
  assigned: number
}

export interface AdminDashboard {
  department: string
  semesterId: string
  semesterName: string
  currentWeek: number
  totalWeeks: number
  activeTas: number
  upcomingQuizzes: number
  quizzesMissingProctors: number
  emptySeats: number
  flaggedAssignments: number
  activeExcuses: number
  tasWithoutSchedule: number
  upcomingSchedule: QuizBrief[]
  mostActive: TaRank[]
  thisWeek: DayLoad[]
}

export interface TaDashboard {
  name: string
  semesterName: string
  currentWeek: number
  points: number
  targetWorkload: number
  maxSessions: number
  sessionsThisSemester: number
  sessionsThisWeek: number
  maxPerWeek: number
  upcoming: Assignment[]
  activeExcuses: Excuse[]
  daysOff: Day[]
  canEditSchedule: boolean
  slotCount: number
}

export interface TaSummary {
  userId: string
  name: string
  email: string
  isActive: boolean
  daysOff: Day[]
  slotCount: number
  canEditSchedule: boolean
  assignments: number
  upcoming: number
  completed: number
  thisWeek: number
  points: number
  targetWorkload: number
  maxSessions: number
  targetWorkloadOverride?: number | null
  maxSessionsOverride?: number | null
  activeExcuses: number
  targetReached: boolean
  coursesTaught: string[]
}

export interface ProctorDetail {
  summary: TaSummary
  history: Assignment[]
  excuses: Excuse[]
}

export interface Person {
  userId: string
  name: string
}

export interface CourseSummary {
  courseId: string
  name: string
  code?: string | null
  quizCount: number
  past: number
  upcoming: number
  totalAssignments: number
  distinctProctors: number
  courseTas: Person[]
  quizzes: QuizBrief[]
}

export interface WeekLoad {
  week: number
  weekStart: string
  quizzes: number
  seatsNeeded: number
  seatsFilled: number
}

export interface Analytics {
  totalQuizzes: number
  totalAssignments: number
  courses: number
  tas: number
  pastQuizzes: number
  upcomingQuizzes: number
  averageAssignments: number
  zeroAssignmentTas: number
  aboveAverage: number
  belowAverage: number
  fairnessSpread: number
  distribution: TaRank[]
  weeks: WeekLoad[]
}

// ── Calendar import
export interface ImportLocationPreview {
  roomName: string
  group?: string | null
  proctorsNeeded: number
  alreadyInQuiz: boolean
  roomKnown: boolean
}

export interface ImportQuizPreview {
  key: string
  courseName: string
  courseId?: string | null
  isNewCourse: boolean
  examType: ExamType
  date: string
  startTime: string
  endTime: string
  slotNumber?: number | null
  slotLabel: string
  weekNumber: number
  fileWeek?: number | null
  existingQuizId?: string | null
  locations: ImportLocationPreview[]
  warnings: string[]
  sourceRows: number[]
}

export interface ImportIssue {
  row: number
  severity: 'Error' | 'Skipped' | 'Info'
  message: string
}

export interface ImportPreview {
  semesterId: string
  semesterName: string
  sheetName?: string | null
  rowsRead: number
  quizzes: ImportQuizPreview[]
  issues: ImportIssue[]
}

export interface ImportCommitResult {
  quizzesCreated: number
  quizzesUpdated: number
  roomsAdded: number
  coursesCreated: string[]
  errors: string[]
  autoAssign?: AutoAssignSummary | null
}
