import { useState } from 'react'
import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { get } from '../../api/client'
import type { AdminDashboard } from '../../api/types'
import { PageHead } from '../../components/AppShell'
import { IconAlert, IconBook, IconCalendar, IconClock, IconExcuse, IconPlus } from '../../components/Icons'
import { ManageCourses } from '../../components/ManageCourses'
import { Empty, ErrorNote, Loading, cx } from '../../components/ui'
import { useUser } from '../../contexts/AuthContext'
import { useTerm } from '../../contexts/TermContext'
import { useQuery } from '../../lib/hooks'
import { SHORT_DAY, dayMonth, dayName, greeting, isFriday, plural, shortDate, timeRange, todayIso } from '../../lib/format'

export default function Overview() {
  const user = useUser()
  const { semester } = useTerm()
  const [coursesOpen, setCoursesOpen] = useState(false)
  const q = useQuery(() => get<AdminDashboard>('/dashboard/admin', { semesterId: semester?.id }), [semester?.id])
  const d = q.data
  const today = todayIso()

  const attention = d
    ? [
        d.quizzesMissingProctors > 0 && {
          to: '/quizzes?status=Upcoming&incomplete=1',
          icon: <IconCalendar />,
          text: `${plural(d.quizzesMissingProctors, 'upcoming quiz', 'upcoming quizzes')} with ${plural(d.emptySeats, 'open seat')}`,
          tone: 'short',
        },
        d.flaggedAssignments > 0 && {
          to: '/quizzes?status=Upcoming',
          icon: <IconAlert />,
          text: `${plural(d.flaggedAssignments, 'assignment')} now ${d.flaggedAssignments === 1 ? 'breaks' : 'break'} a rule (schedule or excuse changed)`,
          tone: 'warn',
        },
        d.activeExcuses > 0 && { to: '/proctors?filter=excuses', icon: <IconExcuse />, text: `${plural(d.activeExcuses, 'active excuse')}`, tone: '' },
        d.tasWithoutSchedule > 0 && { to: '/proctors?filter=noschedule', icon: <IconClock />, text: `${plural(d.tasWithoutSchedule, 'TA')} without a teaching schedule`, tone: '' },
      ].filter(Boolean) as { to: string; icon: ReactNode; text: string; tone: string }[]
    : []

  return (
    <>
      <PageHead
        title={`${greeting()}, ${user.firstName}`}
        sub={d ? `Week ${d.currentWeek} of ${d.totalWeeks} in ${d.semesterName}, ${d.department} department.` : undefined}
      >
        <button className="btn" onClick={() => setCoursesOpen(true)}>
          <IconBook />
          Manage courses
        </button>
        <Link className="btn primary" to="/quizzes/new">
          <IconPlus />
          Add quiz
        </Link>
      </PageHead>

      {q.error && <ErrorNote message={q.error} onRetry={q.reload} />}
      {!d && !q.error && <Loading />}
      {d && (
        <>
          <div className="tiles">
            <Link className="tile" to="/quizzes?status=Upcoming">
              <span className="num">{d.upcomingQuizzes}</span>
              <span className="label">Upcoming quizzes</span>
            </Link>
            <Link className={cx('tile', d.quizzesMissingProctors > 0 ? 'alert' : 'good')} to="/quizzes?status=Upcoming&incomplete=1">
              <span className="num">{d.quizzesMissingProctors}</span>
              <span className="label">Quizzes missing proctors</span>
            </Link>
            <Link className={cx('tile', d.emptySeats > 0 && 'alert')} to="/assign">
              <span className="num">{d.emptySeats}</span>
              <span className="label">Open proctor seats</span>
            </Link>
            <Link className="tile" to="/proctors">
              <span className="num">{d.activeTas}</span>
              <span className="label">Active teaching assistants</span>
            </Link>
          </div>

          <div className="overview">
            <div className="stack" style={{ gap: 18 }}>
              <section className="sheet">
                <div className="sheet-head">
                  <h2>This week</h2>
                  <Link to="/quizzes">All quizzes</Link>
                </div>
                <div className="weekstrip" style={{ gridTemplateColumns: `repeat(${Math.max(1, d.thisWeek.length)}, minmax(0, 1fr))` }}>
                  {d.thisWeek.map((day) => {
                    const fri = isFriday(day.date)
                    const short = day.assigned < day.needed
                    return (
                      <div key={day.date} className={cx('d', day.date === today && 'today', fri && 'fri')}>
                        <b>{SHORT_DAY[dayName(day.date)]}</b>
                        <span>{dayMonth(day.date)}</span>
                        {fri ? (
                          <span>Day off</span>
                        ) : day.quizzes > 0 ? (
                          <>
                            <span className="q">{day.quizzes}</span>
                            <span style={{ color: short ? 'var(--short)' : 'var(--ok)', fontWeight: 600 }}>
                              {day.assigned}/{day.needed} seats
                            </span>
                          </>
                        ) : (
                          <span className="faint">No quizzes</span>
                        )}
                      </div>
                    )
                  })}
                </div>
              </section>

              <section className="sheet">
                <div className="sheet-head">
                  <h2>Coming up</h2>
                  <Link to="/quizzes?status=Upcoming">View all</Link>
                </div>
                {d.upcomingSchedule.length === 0 ? (
                  <Empty icon={<IconCalendar />} title="No upcoming quizzes">
                    <Link to="/import">Import the quiz calendar</Link> to plan the rest of the semester.
                  </Empty>
                ) : (
                  <div className="agenda">
                    {d.upcomingSchedule.map((s) => (
                      <Link key={s.id} to={`/quizzes?focus=${s.id}&status=All`}>
                        <span className="when">
                          <b>{shortDate(s.date)}</b>
                          <span>{timeRange(s.startTime, s.endTime)}</span>
                        </span>
                        <span className="what">
                          <b>
                            {s.courseName}
                            {s.examType !== 'Quiz' ? ` (${s.examType})` : ''}
                          </b>
                          <span>{s.rooms.join(', ')}</span>
                        </span>
                        <span className={cx('tag', s.assigned >= s.needed ? 'ok' : 'short')}>
                          {s.assigned}/{s.needed}
                        </span>
                      </Link>
                    ))}
                  </div>
                )}
              </section>
            </div>

            <div className="stack" style={{ gap: 18 }}>
              <section className="sheet">
                <div className="sheet-head">
                  <h2>Needs attention</h2>
                </div>
                {attention.length === 0 ? (
                  <div className="sheet-pad muted">Nothing needs you right now. Every upcoming seat is filled and no rule is broken.</div>
                ) : (
                  <div className="agenda">
                    {attention.map((a) => (
                      <Link key={a.text} to={a.to} style={{ gridTemplateColumns: '22px minmax(0,1fr)' }}>
                        <span style={{ color: a.tone === 'short' ? 'var(--short)' : a.tone === 'warn' ? 'var(--warn)' : 'var(--ink-3)', display: 'grid' }}>{a.icon}</span>
                        <span className="what">
                          <b style={{ whiteSpace: 'normal' }}>{a.text}</b>
                        </span>
                      </Link>
                    ))}
                  </div>
                )}
              </section>

              <section className="sheet">
                <div className="sheet-head">
                  <h2>Most sessions so far</h2>
                  <Link to="/reviewing">Reviewing</Link>
                </div>
                <div className="leader">
                  {d.mostActive.length === 0 && <div className="muted">No assignments yet.</div>}
                  {d.mostActive.map((t, i) => (
                    <div key={t.userId}>
                      <span className="pos">{i + 1}</span>
                      <Link to={`/proctors?ta=${t.userId}`} style={{ color: 'var(--ink)', fontWeight: 600 }}>
                        {t.name}
                      </Link>
                      <span className="n">{t.assignments}</span>
                    </div>
                  ))}
                </div>
              </section>
            </div>
          </div>
        </>
      )}
      {coursesOpen && <ManageCourses onClose={() => setCoursesOpen(false)} department={d?.department} />}
    </>
  )
}
