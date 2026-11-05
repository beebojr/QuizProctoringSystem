import { DAY_END, DAY_START, SLOTS, hhmm, minutes } from '../lib/format'

export interface RailBlock {
  start: string
  end: string
  label?: string
  title?: string
}

const pct = (t: string) => {
  const m = Math.min(Math.max(minutes(t), DAY_START), DAY_END)
  return ((m - DAY_START) / (DAY_END - DAY_START)) * 100
}
const overlaps = (a: RailBlock, b: RailBlock) => minutes(a.start) < minutes(b.end) && minutes(a.end) > minutes(b.start)

/**
 * One teaching day from 08:30 to 17:15. Classes are ink bars, other bookings are hatched,
 * and the quiz window is the highlighter band — a class inside the band turns red.
 */
export function SlotRail({ teaching = [], busy = [], window: win, label }: { teaching?: RailBlock[]; busy?: RailBlock[]; window?: RailBlock; label?: string }) {
  const desc =
    label ??
    (teaching.length
      ? `Teaching ${teaching.map((t) => `${hhmm(t.start)}–${hhmm(t.end)}`).join(', ')}`
      : 'No teaching that day')
  return (
    <div className="slotrail" role="img" aria-label={desc}>
      {win && <span className="window" style={{ left: `${pct(win.start)}%`, width: `${Math.max(pct(win.end) - pct(win.start), 1.2)}%` }} />}
      {SLOTS.map((s) => (
        <span key={s.number} className="tick" style={{ left: `${pct(s.start)}%` }} />
      ))}
      {busy.map((b, i) => (
        <span key={`b${i}`} className="busy" title={b.title} style={{ left: `${pct(b.start)}%`, width: `${pct(b.end) - pct(b.start)}%` }} />
      ))}
      {teaching.map((t, i) => (
        <span
          key={`t${i}`}
          className={'teach' + (win && overlaps(t, win) ? ' clash' : '')}
          title={t.title ?? t.label}
          style={{ left: `${pct(t.start)}%`, width: `${pct(t.end) - pct(t.start)}%` }}
        >
          {t.label}
        </span>
      ))}
      {win && <span className="window-edge" style={{ left: `${pct(win.start)}%`, width: `${Math.max(pct(win.end) - pct(win.start), 1.2)}%` }} />}
    </div>
  )
}

export function SlotRailScale() {
  const marks = ['08:30', '10:15', '12:00', '13:45', '15:45', '17:15']
  return (
    <div className="slotrail-scale" aria-hidden="true">
      {marks.map((m, i) => (
        <span key={m} style={{ left: `${pct(m)}%`, translate: i === 0 ? '0 0' : i === marks.length - 1 ? '-100% 0' : undefined }}>
          {m}
        </span>
      ))}
    </div>
  )
}
