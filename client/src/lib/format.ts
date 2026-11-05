// Dates arrive as "yyyy-MM-dd" and times as "HH:mm:ss" (local Cairo time, no zone).
import type { Day } from '../api/types'

const DAY_NAMES: Day[] = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']
/** Teaching days in university order (Friday is off for everyone). */
export const TEACHING_DAYS: Day[] = ['Saturday', 'Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday']
export const SHORT_DAY: Record<Day, string> = {
  Saturday: 'Sat', Sunday: 'Sun', Monday: 'Mon', Tuesday: 'Tue', Wednesday: 'Wed', Thursday: 'Thu', Friday: 'Fri',
}

export interface SlotDef {
  number: number
  label: string
  name: string
  start: string
  end: string
}

/** The university's fixed periods. The 30-minute gap is used for short quizzes. */
export const SLOTS: SlotDef[] = [
  { number: 1, label: '1st', name: '1st slot', start: '08:30', end: '10:00' },
  { number: 2, label: '2nd', name: '2nd slot', start: '10:15', end: '11:45' },
  { number: 3, label: '3rd', name: '3rd slot', start: '12:00', end: '13:30' },
  { number: 4, label: '4th', name: '4th slot', start: '13:45', end: '15:15' },
  { number: 0, label: 'Gap', name: 'Gap', start: '15:15', end: '15:45' },
  { number: 5, label: '5th', name: '5th slot', start: '15:45', end: '17:15' },
]
export const TEACHING_SLOTS = SLOTS.filter((s) => s.number !== 0)
export const DAY_START = 8 * 60 + 30
export const DAY_END = 17 * 60 + 15

export function slotLabel(n?: number | null): string {
  if (n === null || n === undefined) return 'Custom'
  return SLOTS.find((s) => s.number === n)?.label ?? `Slot ${n}`
}

/** "5TH" → "5th", "GAP" → "Gap" (labels coming from the server). */
export function prettySlot(label: string): string {
  return label.length ? label[0] + label.slice(1).toLowerCase() : label
}

export function parseDate(s: string): Date {
  const [y, m, d] = s.slice(0, 10).split('-').map(Number)
  return new Date(y, m - 1, d)
}

export function isoDate(d: Date): string {
  const p = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`
}

export function todayIso(): string {
  return isoDate(new Date())
}

export function addDays(iso: string, n: number): string {
  const d = parseDate(iso)
  d.setDate(d.getDate() + n)
  return isoDate(d)
}

export function dayName(iso: string): Day {
  return DAY_NAMES[parseDate(iso).getDay()]
}

export function isFriday(iso: string): boolean {
  return parseDate(iso).getDay() === 5
}

const fmt = (iso: string, o: Intl.DateTimeFormatOptions) => parseDate(iso).toLocaleDateString('en-GB', o)

/** "Tue 4 Nov" */
export const shortDate = (iso: string) => fmt(iso, { weekday: 'short', day: 'numeric', month: 'short' })
/** "4 Nov" */
export const dayMonth = (iso: string) => fmt(iso, { day: 'numeric', month: 'short' })
/** "Tuesday 4 November" */
export const longDate = (iso: string) => fmt(iso, { weekday: 'long', day: 'numeric', month: 'long' })
/** "4 Nov 2026" */
export const fullDate = (iso: string) => fmt(iso, { day: 'numeric', month: 'short', year: 'numeric' })

export function minutes(t: string): number {
  const [h, m] = t.split(':').map(Number)
  return h * 60 + (m || 0)
}

export function hhmm(t: string): string {
  return t.slice(0, 5)
}

/** "3:45 PM" */
export function time12(t: string): string {
  const total = minutes(t)
  const h = Math.floor(total / 60)
  const m = total % 60
  const suffix = h >= 12 ? 'PM' : 'AM'
  const h12 = h % 12 === 0 ? 12 : h % 12
  return `${h12}:${String(m).padStart(2, '0')} ${suffix}`
}

/** "3:45–5:15 PM" or "11:15 AM–12:45 PM" */
export function timeRange(start: string, end: string): string {
  const a = time12(start)
  const b = time12(end)
  if (a.slice(-2) === b.slice(-2)) return `${a.slice(0, -3)}–${b}`
  return `${a}–${b}`
}

export function slotForTimes(start: string, end: string): SlotDef | undefined {
  return SLOTS.find((s) => s.start === hhmm(start) && s.end === hhmm(end))
}

/** Academic week = 7-day blocks counted from the semester's first day. */
export function academicWeek(semesterStart: string, iso: string): number {
  const days = Math.round((parseDate(iso).getTime() - parseDate(semesterStart).getTime()) / 86400000)
  return Math.floor(days / 7) + 1
}

export function relativeTime(isoDateTime: string): string {
  const then = new Date(isoDateTime.endsWith('Z') || isoDateTime.includes('+') ? isoDateTime : isoDateTime + 'Z')
  const s = Math.round((Date.now() - then.getTime()) / 1000)
  if (s < 60) return 'just now'
  if (s < 3600) return `${Math.floor(s / 60)} min ago`
  if (s < 86400) return `${Math.floor(s / 3600)} h ago`
  if (s < 86400 * 7) return `${Math.floor(s / 86400)} d ago`
  return then.toLocaleDateString('en-GB', { day: 'numeric', month: 'short' })
}

export function initials(name: string): string {
  const parts = name.trim().split(/\s+/)
  return ((parts[0]?.[0] ?? '') + (parts.length > 1 ? parts[parts.length - 1][0] : '')).toUpperCase()
}

export function plural(n: number, one: string, many = one + 's'): string {
  return `${n} ${n === 1 ? one : many}`
}

export function points(n: number): string {
  return Number.isInteger(n) ? String(n) : n.toFixed(1)
}

export function greeting(): string {
  const h = new Date().getHours()
  return h < 12 ? 'Good morning' : h < 17 ? 'Good afternoon' : 'Good evening'
}
