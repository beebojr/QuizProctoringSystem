import { useMemo, useState } from 'react'
import toast from 'react-hot-toast'
import { Link } from 'react-router-dom'
import { errorMessage, post, send } from '../../api/client'
import type { BatchItem, BatchPreview } from '../../api/types'
import { PageHead } from '../../components/AppShell'
import { IconAlert, IconBolt, IconCheck, IconFlag } from '../../components/Icons'
import { announceChange } from '../../components/NotificationBell'
import { Empty, Field, Segmented, cx } from '../../components/ui'
import { useTerm } from '../../contexts/TermContext'
import { addDays, longDate, plural, shortDate, timeRange, todayIso } from '../../lib/format'

type Range = '7' | '14' | 'rest' | 'custom'

export default function AssignmentRun() {
  const { semester, semesterId, settings } = useTerm()
  const today = todayIso()
  const [range, setRange] = useState<Range>('14')
  const [from, setFrom] = useState(today)
  const [to, setTo] = useState(addDays(today, 13))
  const [plan, setPlan] = useState<BatchPreview | null>(null)
  const [skip, setSkip] = useState<Set<string>>(new Set())
  const [busy, setBusy] = useState(false)
  const [done, setDone] = useState<{ assigned: number; rejected: string[] } | null>(null)

  const dateWindow = (): { from?: string; to?: string } => {
    if (range === 'custom') return { from, to }
    if (range === 'rest') return { from: today, to: semester?.endDate }
    return { from: today, to: addDays(today, Number(range) - 1) }
  }

  const preview = async () => {
    setBusy(true)
    setDone(null)
    try {
      const w = dateWindow()
      const p = await post<BatchPreview>('/assignments/auto-assign/preview', { semesterId, from: w.from, to: w.to })
      setPlan(p)
      setSkip(new Set())
    } catch (e) {
      toast.error(errorMessage(e))
    } finally {
      setBusy(false)
    }
  }

  const itemKey = (i: BatchItem) => `${i.locationId}:${i.userId}`
  const chosen = plan?.items.filter((i) => !skip.has(itemKey(i))) ?? []

  const commit = async () => {
    if (!plan || chosen.length === 0) return
    setBusy(true)
    try {
      const r = await send<{ assigned: number; rejected: string[] }>('post', '/assignments/auto-assign/commit', {
        semesterId,
        items: chosen.map((i) => ({ locationId: i.locationId, userId: i.userId })),
      })
      setDone(r)
      setPlan(null)
      announceChange()
      if (r.rejected.length === 0) toast.success(`${plural(r.assigned, 'proctor')} assigned and notified.`)
    } catch (e) {
      toast.error(errorMessage(e))
    } finally {
      setBusy(false)
    }
  }

  const byQuiz = useMemo(() => {
    const map = new Map<string, BatchItem[]>()
    for (const i of plan?.items ?? []) map.set(i.quizId, [...(map.get(i.quizId) ?? []), i])
    return [...map.values()].sort((a, b) => a[0].date.localeCompare(b[0].date) || a[0].startTime.localeCompare(b[0].startTime))
  }, [plan])

  const perTa = useMemo(() => {
    const map = new Map<string, number>()
    for (const i of chosen) map.set(i.taName, (map.get(i.taName) ?? 0) + 1)
    return [...map.entries()].sort((a, b) => b[1] - a[1] || a[0].localeCompare(b[0]))
  }, [chosen])

  const toggle = (i: BatchItem) => {
    const next = new Set(skip)
    const k = itemKey(i)
    if (next.has(k)) next.delete(k)
    else next.add(k)
    setSkip(next)
  }

  return (
    <>
      <PageHead
        title="Assignment run"
        sub={`Fills every open seat in a date range in one go. Course TAs are placed first, one per room; the rest go to whoever is furthest from their target. Nobody gets more than ${settings?.maxSessionsPerWeek ?? 2} sessions a week or more than their semester limit.`}
      />

      <section className="sheet sheet-pad row-wrap" style={{ alignItems: 'flex-end', gap: 16, marginBottom: 18 }}>
        <Field label="Quizzes from">
          <Segmented
            value={range}
            onChange={(r) => { setRange(r); setPlan(null) }}
            options={[
              { value: '7' as Range, label: 'Next 7 days' },
              { value: '14' as Range, label: 'Next 14 days' },
              { value: 'rest' as Range, label: 'Rest of semester' },
              { value: 'custom' as Range, label: 'Dates' },
            ]}
            label="Range"
          />
        </Field>
        {range === 'custom' && (
          <>
            <Field label="From">
              <input className="input" type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
            </Field>
            <Field label="To">
              <input className="input" type="date" value={to} min={from} onChange={(e) => setTo(e.target.value)} />
            </Field>
          </>
        )}
        <span className="grow" />
        <button className="btn primary" onClick={preview} disabled={busy}>
          <IconBolt />
          {busy && !plan ? 'Planning…' : 'Plan assignments'}
        </button>
      </section>

      {done && (
        <div className={cx('notice', done.rejected.length ? 'warn' : 'ok')} style={{ marginBottom: 18 }}>
          <IconCheck />
          <div>
            {plural(done.assigned, 'proctor')} assigned and notified.
            {done.rejected.length > 0 && (
              <>
                {' '}These changed since the plan and were skipped:
                <ul>{done.rejected.map((r) => <li key={r}>{r}</li>)}</ul>
              </>
            )}{' '}
            <Link to="/quizzes?status=Upcoming">Open the quiz board</Link>
          </div>
        </div>
      )}

      {!plan && !done && (
        <div className="sheet">
          <Empty icon={<IconBolt />} title="Nothing planned yet">
            Choose a range and plan. You see every proposed assignment before anything is saved.
          </Empty>
        </div>
      )}

      {plan && (
        <div className="overview">
          <div className="stack">
            <div className="tiles" style={{ gridTemplateColumns: 'repeat(3, minmax(0,1fr))', marginBottom: 0 }}>
              <div className="tile"><span className="num">{plan.quizCount}</span><span className="label">Quizzes with open seats</span></div>
              <div className="tile"><span className="num">{plan.emptySeats}</span><span className="label">Open seats</span></div>
              <div className={cx('tile', plan.items.length === plan.emptySeats ? 'good' : 'alert')}>
                <span className="num">{plan.items.length}<small> / {plan.emptySeats}</small></span>
                <span className="label">Seats the plan fills</span>
              </div>
            </div>

            {plan.shortfalls.length > 0 && (
              <section className="sheet">
                <div className="sheet-head">
                  <h3 style={{ color: 'var(--short)' }}><IconAlert width={17} height={17} />Seats nobody can take</h3>
                </div>
                <div className="sheet-pad stack-s" style={{ paddingTop: 10 }}>
                  {plan.shortfalls.map((s) => (
                    <div key={s.locationId} className="small" style={{ paddingBottom: 8, borderBottom: '1px solid var(--line)' }}>
                      <b>{s.courseName}</b>, {shortDate(s.date)}, {timeRange(s.startTime, s.endTime)}, <span className="room">{s.roomName}</span>: {plural(s.missing, 'seat')} open.
                      <div className="muted">Everyone else is {s.reasons.join('; ')}.</div>
                    </div>
                  ))}
                  <p className="small muted">Fill these by hand from the quiz board, where you can override a rule with a reason.</p>
                </div>
              </section>
            )}

            {plan.items.length === 0 ? (
              <div className="sheet">
                <Empty icon={<IconCheck />} title={plan.emptySeats === 0 ? 'Every seat in this range is already filled' : 'No seat can be filled without breaking a rule'} />
              </div>
            ) : (
              <section className="sheet">
                <div className="sheet-head">
                  <h3>Proposed assignments</h3>
                  <span className="small muted">Untick anyone you don't want.</span>
                </div>
                {byQuiz.map((items) => {
                  const first = items[0]
                  return (
                    <div key={first.quizId} className="batch-quiz">
                      <div className="spread">
                        <b>{first.courseName}</b>
                        <span className="small muted">{longDate(first.date)}, {timeRange(first.startTime, first.endTime)}, week {first.weekNumber}</span>
                      </div>
                      <div className="batch-items">
                        {items.map((i) => (
                          <label key={itemKey(i)} className="batch-item check">
                            <input type="checkbox" checked={!skip.has(itemKey(i))} onChange={() => toggle(i)} />
                            <span className="room">{i.isReserve ? 'Reserve' : i.roomName}</span>
                            <span className={cx(skip.has(itemKey(i)) && 'faint')}>
                              {i.taName}
                              {i.isCourseTa && <IconFlag width={13} height={13} style={{ color: 'var(--ta)', marginLeft: 6, verticalAlign: -2 }} aria-label="Teaches this course" />}
                            </span>
                          </label>
                        ))}
                      </div>
                    </div>
                  )
                })}
              </section>
            )}
          </div>

          <aside className="stack" style={{ position: 'sticky', top: 64 }}>
            <section className="sheet sheet-pad stack">
              <h3>Confirm</h3>
              <p className="small muted">Each TA gets a notification with the course, date, time and room.</p>
              <button className="btn mark block" onClick={commit} disabled={busy || chosen.length === 0}>
                {busy ? 'Assigning…' : `Assign ${plural(chosen.length, 'proctor')}`}
              </button>
              <button className="btn ghost block" onClick={() => setPlan(null)}>Discard plan</button>
            </section>
            {perTa.length > 0 && (
              <section className="sheet">
                <div className="sheet-head"><h3>Who gets what</h3></div>
                <div className="leader" style={{ maxHeight: 360, overflowY: 'auto' }}>
                  {perTa.map(([name, n]) => (
                    <div key={name} style={{ gridTemplateColumns: 'minmax(0,1fr) auto' }}>
                      <span>{name}</span>
                      <span className="n">+{n}</span>
                    </div>
                  ))}
                </div>
              </section>
            )}
          </aside>
        </div>
      )}
    </>
  )
}
