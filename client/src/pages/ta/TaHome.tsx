import { Link } from 'react-router-dom'
import { get } from '../../api/client'
import type { TaDashboard } from '../../api/types'
import { PageHead } from '../../components/AppShell'
import { AssignmentRow, NextTicket } from '../../components/AssignmentTicket'
import { IconAlert, IconCalendar } from '../../components/Icons'
import { Empty, ErrorNote, Loading, cx } from '../../components/ui'
import { useQuery } from '../../lib/hooks'
import { dayMonth, greeting, plural, points } from '../../lib/format'

export default function TaHome() {
  const q = useQuery(() => get<TaDashboard>('/dashboard/ta'), [])
  if (q.error) return <ErrorNote message={q.error} onRetry={q.reload} />
  const d = q.data
  if (!d) return <Loading />
  const [next, ...rest] = d.upcoming
  const ratio = d.targetWorkload > 0 ? d.points / d.targetWorkload : 0

  return (
    <>
      <PageHead title={`${greeting()}, ${d.name.split(' ')[0]}`} sub={`Week ${d.currentWeek} of ${d.semesterName}.`} />

      {d.slotCount === 0 && (
        <div className="notice warn" style={{ marginBottom: 16 }}>
          <IconAlert />
          <span>
            Your teaching schedule is empty, so you could be assigned during your classes.{' '}
            {d.canEditSchedule ? <Link to="/my-schedule">Add your classes</Link> : 'Ask an administrator to enter it.'}
          </span>
        </div>
      )}

      <div className="tiles">
        <div className={cx('tile', d.sessionsThisWeek >= d.maxPerWeek && 'alert')}>
          <span className="num">{d.sessionsThisWeek}<small> / {d.maxPerWeek}</small></span>
          <span className="label">Sessions this week</span>
        </div>
        <div className="tile">
          <span className="num">{d.sessionsThisSemester}<small> / {d.maxSessions}</small></span>
          <span className="label">Sessions this semester</span>
        </div>
        <div className={cx('tile', ratio >= 1 && 'good')}>
          <span className="num">{points(d.points)}<small> / {points(d.targetWorkload)}</small></span>
          <span className="label">Workload points</span>
        </div>
        <Link className="tile" to="/my-schedule">
          <span className="num">{d.slotCount}</span>
          <span className="label">Classes in your timetable{d.canEditSchedule ? '' : ' (locked)'}</span>
        </Link>
      </div>

      <div className="overview">
        <div className="stack" style={{ gap: 18 }}>
          {next ? (
            <NextTicket a={next} />
          ) : (
            <div className="sheet">
              <Empty icon={<IconCalendar />} title="No upcoming proctoring">You are notified here as soon as you are assigned.</Empty>
            </div>
          )}
          {rest.length > 0 && (
            <section className="sheet">
              <div className="sheet-head">
                <h2>After that</h2>
                <Link to="/my-assignments">All sessions</Link>
              </div>
              <div className="sheet-pad history" style={{ paddingTop: 4, paddingBottom: 4 }}>
                {rest.map((a) => <AssignmentRow key={a.assignmentId} a={a} />)}
              </div>
            </section>
          )}
        </div>
        <div className="stack" style={{ gap: 18 }}>
          <section className="sheet sheet-pad stack-s">
            <h3>How you are assigned</h3>
            <p className="small muted">
              Never on Fridays{d.daysOff.length ? ` or your day off (${d.daysOff.join(', ')})` : ''}, never during your classes, at most {plural(d.maxPerWeek, 'session')} a week
              and {d.maxSessions} this semester. Sessions go first to those furthest from their target.
            </p>
          </section>
          <section className="sheet">
            <div className="sheet-head">
              <h3>Excuses</h3>
              <Link to="/my-excuses">Submit one</Link>
            </div>
            <div className="sheet-pad stack-s" style={{ paddingTop: 10 }}>
              {d.activeExcuses.length === 0 && <p className="small muted">None active.</p>}
              {d.activeExcuses.map((x) => (
                <div key={x.id} className="small">
                  <b>{dayMonth(x.startDate)}{x.endDate !== x.startDate ? ` – ${dayMonth(x.endDate)}` : ''}</b>
                  <div className="muted">{x.reason}</div>
                </div>
              ))}
            </div>
          </section>
        </div>
      </div>
    </>
  )
}
