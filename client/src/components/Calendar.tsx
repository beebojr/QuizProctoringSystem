import { useMemo, useState } from 'react'
import { isoDate, parseDate, todayIso } from '../lib/format'
import { IconChevronLeft, IconChevronRight } from './Icons'
import { cx } from './ui'

const DOW = ['Sat', 'Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri']

/** Month picker that starts on Saturday (the university week) and blocks Fridays. */
export function Calendar({ value, onChange, min, max, marked }: { value: string | null; onChange: (iso: string) => void; min?: string; max?: string; marked?: Set<string> }) {
  const initial = value ?? (min && todayIso() < min ? min : todayIso())
  const [cursor, setCursor] = useState(() => {
    const d = parseDate(initial)
    return new Date(d.getFullYear(), d.getMonth(), 1)
  })
  const today = todayIso()

  const days = useMemo(() => {
    const first = new Date(cursor)
    const offset = (first.getDay() + 1) % 7 // Saturday = 0
    const start = new Date(first)
    start.setDate(1 - offset)
    return Array.from({ length: 42 }, (_, i) => {
      const d = new Date(start)
      d.setDate(start.getDate() + i)
      return d
    })
  }, [cursor])

  const shift = (n: number) => setCursor((c) => new Date(c.getFullYear(), c.getMonth() + n, 1))
  const trimmed = days[35].getMonth() !== cursor.getMonth() ? days.slice(0, 35) : days

  return (
    <div className="cal">
      <div className="cal-head">
        <button type="button" className="icon-btn" onClick={() => shift(-1)} aria-label="Previous month">
          <IconChevronLeft />
        </button>
        <b>{cursor.toLocaleDateString('en-GB', { month: 'long', year: 'numeric' })}</b>
        <button type="button" className="icon-btn" onClick={() => shift(1)} aria-label="Next month">
          <IconChevronRight />
        </button>
      </div>
      <div className="cal-grid">
        {DOW.map((d) => (
          <span key={d} className="dow">
            {d}
          </span>
        ))}
        {trimmed.map((d) => {
          const iso = isoDate(d)
          const friday = d.getDay() === 5
          const outside = d.getMonth() !== cursor.getMonth()
          const outOfRange = (!!min && iso < min) || (!!max && iso > max)
          return (
            <button
              type="button"
              key={iso}
              disabled={friday || outOfRange}
              className={cx(friday && 'friday', outside && 'out', iso === today && 'today', iso === value && 'picked')}
              aria-pressed={iso === value}
              aria-label={d.toLocaleDateString('en-GB', { weekday: 'long', day: 'numeric', month: 'long' }) + (friday ? ', Friday off' : '')}
              onClick={() => onChange(iso)}
            >
              {d.getDate()}
              {marked?.has(iso) && <i />}
            </button>
          )
        })}
      </div>
    </div>
  )
}
