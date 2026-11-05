import { useMemo, useState } from 'react'
import toast from 'react-hot-toast'
import { Link } from 'react-router-dom'
import { download, errorMessage, get } from '../../api/client'
import type { Analytics, Assignment, CourseSummary, TaSummary } from '../../api/types'
import { PageHead } from '../../components/AppShell'
import { IconBook, IconChart, IconChevronRight, IconDownload, IconEdit, IconUsers } from '../../components/Icons'
import { WorkloadModal } from '../../components/PeopleForms'
import { Empty, ErrorNote, Loading, SearchBox, Segmented, Tabs, cx } from '../../components/ui'
import { useTerm } from '../../contexts/TermContext'
import { useLocalState, useQuery } from '../../lib/hooks'
import { dayMonth, plural, points, shortDate, time12, timeRange } from '../../lib/format'

type Tab = 'tas' | 'courses' | 'analytics'

export default function Reviewing() {
  const [tab, setTab] = useLocalState<Tab>('qps.reviewTab', 'tas')
  const { semesterId } = useTerm()
  return (
    <>
      <PageHead title="Reviewing" sub="How proctoring is shared out across TAs and courses this semester.">
        <button className="btn" onClick={() => download('/reports/export/workload', { semesterId }, 'workload.xlsx').catch((e) => toast.error(errorMessage(e)))}>
          <IconDownload />
          Export workload
        </button>
      </PageHead>
      <Tabs
        value={tab}
        onChange={setTab}
        tabs={[
          { value: 'tas', label: 'TAs', icon: <IconUsers /> },
          { value: 'courses', label: 'Courses', icon: <IconBook /> },
          { value: 'analytics', label: 'Analytics', icon: <IconChart /> },
        ]}
      />
      {tab === 'tas' && <TaTab semesterId={semesterId} />}
      {tab === 'courses' && <CourseTab semesterId={semesterId} />}
      {tab === 'analytics' && <AnalyticsTab semesterId={semesterId} />}
    </>
  )
}

type TaSort = 'most' | 'least' | 'target' | 'name'

function TaTab({ semesterId }: { semesterId?: string }) {
  const tas = useQuery(() => get<TaSummary[]>('/reports/tas', { semesterId }), [semesterId])
  const assignments = useQuery(() => get<Assignment[]>('/assignments', { semesterId }), [semesterId])
  const [search, setSearch] = useState('')
  const [sort, setSort] = useState<TaSort>('most')
  const [editing, setEditing] = useState<TaSummary | null>(null)

  const byTa = useMemo(() => {
    const map = new Map<string, Assignment[]>()
    for (const a of assignments.data ?? []) map.set(a.userId, [...(map.get(a.userId) ?? []), a])
    for (const list of map.values()) list.sort((a, b) => b.date.localeCompare(a.date) || b.startTime.localeCompare(a.startTime))
    return map
  }, [assignments.data])

  const list = useMemo(() => {
    const q = search.trim().toLowerCase()
    return (tas.data ?? [])
      .filter((t) => t.isActive || t.assignments > 0)
      .filter((t) => {
        if (!q) return true
        if (t.name.toLowerCase().includes(q) || t.email.toLowerCase().includes(q)) return true
        return (byTa.get(t.userId) ?? []).some((a) => a.courseName.toLowerCase().includes(q) || a.roomName.toLowerCase().includes(q))
      })
      .sort((a, b) => {
        if (sort === 'name') return a.name.localeCompare(b.name)
        if (sort === 'least') return a.points - b.points || a.name.localeCompare(b.name)
        if (sort === 'target') return Number(b.targetReached) - Number(a.targetReached) || b.points / (b.targetWorkload || 1) - a.points / (a.targetWorkload || 1)
        return b.points - a.points || a.name.localeCompare(b.name)
      })
  }, [tas.data, byTa, search, sort])

  if (tas.error) return <ErrorNote message={tas.error} onRetry={tas.reload} />
  if (!tas.data) return <Loading />
  return (
    <div className="stack">
      <div className="toolbar" style={{ marginBottom: 4 }}>
        <SearchBox value={search} onChange={setSearch} placeholder="Name, email, course or room" className="grow" />
        <Segmented
          value={sort}
          onChange={setSort}
          options={[
            { value: 'most' as TaSort, label: 'Most points' },
            { value: 'least' as TaSort, label: 'Fewest points' },
            { value: 'target' as TaSort, label: 'Target reached' },
            { value: 'name' as TaSort, label: 'By name' },
          ]}
          label="Sort"
        />
      </div>
      {list.length === 0 && <div className="sheet"><Empty icon={<IconUsers />} title="Nobody matches the search" /></div>}
      <div className="cards">
        {list.map((t) => {
          const history = byTa.get(t.userId) ?? []
          const ratio = t.targetWorkload > 0 ? t.points / t.targetWorkload : 0
          return (
            <article key={t.userId} className="sheet ta-card">
              <div className="spread" style={{ alignItems: 'flex-start' }}>
                <div style={{ minWidth: 0 }}>
                  <Link to={`/proctors?ta=${t.userId}`} style={{ color: 'var(--ink)' }}><h3>{t.name}</h3></Link>
                  <span className="small muted">{t.email}</span>
                </div>
                <button className="row" style={{ border: 0, background: 'none', padding: 0 }} onClick={() => setEditing(t)} title="Adjust target and limit" aria-label={`Adjust workload of ${t.name}`}>
                  <span className="score">
                    {points(t.points)} <small>/ {points(t.targetWorkload)}</small>
                  </span>
                  <IconEdit width={14} height={14} style={{ color: 'var(--ink-4)' }} />
                </button>
              </div>
              <div className="load-bar">
                <i className={cx(ratio >= 1 && 'done', t.assignments > t.maxSessions && 'over')} style={{ width: `${Math.min(100, ratio * 100)}%` }} />
              </div>
              <div className="spread small">
                <span className="muted">{t.completed} done, {t.upcoming} upcoming</span>
                {t.targetReached ? <span className="tag ok">Target reached</span> : <span className="muted">{points(Math.max(0, t.targetWorkload - t.points))} to go</span>}
              </div>
              <div className="mini-history">
                {history.length === 0 && <div className="muted">No sessions yet.</div>}
                {history.map((a) => (
                  <div key={a.assignmentId}>
                    <b title={a.courseName}>{a.courseName}</b>
                    <span className="nowrap" style={{ color: a.status === 'Upcoming' ? 'var(--ta)' : 'var(--ink-3)' }}>
                      {dayMonth(a.date)}, {time12(a.startTime)}, {a.isReserve ? 'Reserve' : a.roomName}
                    </span>
                  </div>
                ))}
              </div>
            </article>
          )
        })}
      </div>
      {editing && <WorkloadModal summary={editing} onClose={() => setEditing(null)} onSaved={tas.reload} />}
    </div>
  )
}

type CourseSort = 'most' | 'least' | 'proctors' | 'name'

function CourseTab({ semesterId }: { semesterId?: string }) {
  const courses = useQuery(() => get<CourseSummary[]>('/reports/courses', { semesterId }), [semesterId])
  const [search, setSearch] = useState('')
  const [sort, setSort] = useState<CourseSort>('most')
  const list = useMemo(() => {
    const q = search.trim().toLowerCase()
    return (courses.data ?? [])
      .filter((c) => !q || c.name.toLowerCase().includes(q) || c.courseTas.some((t) => t.name.toLowerCase().includes(q)) || c.quizzes.some((x) => x.rooms.some((r) => r.toLowerCase().includes(q))))
      .sort((a, b) =>
        sort === 'name' ? a.name.localeCompare(b.name) : sort === 'least' ? a.quizCount - b.quizCount : sort === 'proctors' ? b.distinctProctors - a.distinctProctors : b.quizCount - a.quizCount,
      )
  }, [courses.data, search, sort])

  if (courses.error) return <ErrorNote message={courses.error} onRetry={courses.reload} />
  if (!courses.data) return <Loading />
  return (
    <div className="stack">
      <div className="toolbar" style={{ marginBottom: 4 }}>
        <SearchBox value={search} onChange={setSearch} placeholder="Course, course TA or room" className="grow" />
        <Segmented
          value={sort}
          onChange={setSort}
          options={[
            { value: 'most' as CourseSort, label: 'Most quizzes' },
            { value: 'least' as CourseSort, label: 'Fewest quizzes' },
            { value: 'proctors' as CourseSort, label: 'Most proctors' },
            { value: 'name' as CourseSort, label: 'By name' },
          ]}
          label="Sort"
        />
      </div>
      {list.length === 0 && <div className="sheet"><Empty icon={<IconBook />} title="No course matches" /></div>}
      <div className="cards" style={{ gridTemplateColumns: 'repeat(auto-fill, minmax(380px, 1fr))' }}>
        {list.map((c) => (
          <article key={c.courseId} className="sheet course-card">
            <div>
              <h3>{c.name}{c.code ? <span className="faint" style={{ fontWeight: 500 }}> {c.code}</span> : null}</h3>
              <span className="small muted">
                {plural(c.totalAssignments, 'assignment')} shared by {plural(c.distinctProctors, 'proctor')}
              </span>
            </div>
            <div className="kv">
              <div><b>{c.quizCount}</b><span>Quizzes</span></div>
              <div><b style={{ color: 'var(--ok)' }}>{c.past}</b><span>Held</span></div>
              <div><b style={{ color: 'var(--ta)' }}>{c.upcoming}</b><span>Upcoming</span></div>
            </div>
            <details className="fold">
              <summary><IconChevronRight />Course TAs ({c.courseTas.length})</summary>
              <div className="fold-body">
                {c.courseTas.length === 0 && <span className="muted">No TA has this course in their timetable.</span>}
                {c.courseTas.map((t) => (
                  <Link key={t.userId} to={`/proctors?ta=${t.userId}`}>{t.name}</Link>
                ))}
              </div>
            </details>
            <details className="fold">
              <summary><IconChevronRight />Quizzes ({c.quizzes.length})</summary>
              <div className="fold-body">
                {c.quizzes.map((x) => (
                  <Link key={x.id} to={`/quizzes?focus=${x.id}&status=All`} className="spread" style={{ color: 'var(--ink)' }}>
                    <span>
                      {shortDate(x.date)}, {timeRange(x.startTime, x.endTime)}
                      {x.examType !== 'Quiz' ? ` (${x.examType})` : ''}
                    </span>
                    <span className={cx('tag', x.assigned >= x.needed ? 'ok' : 'short')}>{x.assigned}/{x.needed}</span>
                  </Link>
                ))}
              </div>
            </details>
          </article>
        ))}
      </div>
    </div>
  )
}

function AnalyticsTab({ semesterId }: { semesterId?: string }) {
  const { semester } = useTerm()
  const q = useQuery(() => get<Analytics>('/reports/analytics', { semesterId }), [semesterId])
  if (q.error) return <ErrorNote message={q.error} onRetry={q.reload} />
  const a = q.data
  if (!a) return <Loading />
  const ranked = [...a.distribution].sort((x, y) => y.assignments - x.assignments || x.name.localeCompare(y.name))
  const max = Math.max(1, ...ranked.map((r) => r.assignments))
  return (
    <div className="stack" style={{ gap: 18 }}>
      <div className="tiles" style={{ gridTemplateColumns: 'repeat(6, minmax(0, 1fr))', marginBottom: 0 }}>
        <div className="tile"><span className="num">{a.totalQuizzes}</span><span className="label">Quizzes</span></div>
        <div className="tile"><span className="num">{a.pastQuizzes}</span><span className="label">Held so far</span></div>
        <div className="tile"><span className="num">{a.upcomingQuizzes}</span><span className="label">Still to come</span></div>
        <div className="tile"><span className="num">{a.totalAssignments}</span><span className="label">Assignments</span></div>
        <div className="tile"><span className="num">{a.tas}</span><span className="label">TAs</span></div>
        <div className="tile"><span className="num">{a.courses}</span><span className="label">Courses</span></div>
      </div>

      <section className="sheet">
        <div className="sheet-head" style={{ flexWrap: 'wrap' }}>
          <div>
            <h2>{a.averageAssignments.toFixed(2)} sessions per TA on average</h2>
            <span className="small muted">
              The gap between the busiest and the least busy TA is {a.fairnessSpread}.{a.zeroAssignmentTas > 0 ? ` ${plural(a.zeroAssignmentTas, 'TA')} ${a.zeroAssignmentTas === 1 ? 'has' : 'have'} none yet.` : ''}
            </span>
          </div>
          <div className="legend">
            <span><i style={{ background: 'var(--ok)', borderRadius: '50%' }} />At or above average ({a.aboveAverage})</span>
            <span><i style={{ background: 'var(--short)', borderRadius: '50%' }} />Below average ({a.belowAverage})</span>
          </div>
        </div>
        <div className="sheet-pad">
          <SpreadChart data={a.distribution} average={a.averageAssignments} />
        </div>
      </section>

      <div className="overview" style={{ gridTemplateColumns: 'minmax(0, 1.2fr) minmax(0, 1fr)' }}>
        <section className="sheet">
          <div className="sheet-head">
            <h2>Seats per week</h2>
            <div className="legend">
              <span><i style={{ background: 'var(--ink-2)' }} />Filled</span>
              <span><i style={{ background: 'var(--line)' }} />Needed</span>
            </div>
          </div>
          <div className="sheet-pad">
            <WeekChart weeks={a.weeks} currentWeek={semester?.currentWeek ?? 0} />
          </div>
        </section>
        <section className="sheet">
          <div className="sheet-head">
            <h2>TAs by sessions</h2>
            <span className="small muted">Highest first</span>
          </div>
          <div className="sheet-pad rank" style={{ maxHeight: 420, overflowY: 'auto', paddingTop: 4 }}>
            {ranked.map((r, i) => (
              <div key={r.userId}>
                <span className="pos">{i + 1}</span>
                <Link to={`/proctors?ta=${r.userId}`} style={{ color: 'var(--ink)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{r.name}</Link>
                <span className="bar"><i className={cx(r.assignments < a.averageAssignments && 'below')} style={{ width: `${(r.assignments / max) * 100}%` }} /></span>
                <span className="n">{r.assignments}</span>
              </div>
            ))}
          </div>
        </section>
      </div>
    </div>
  )
}

/** Every TA as a dot, sorted low to high, against the average line. */
function SpreadChart({ data, average }: { data: { userId: string; name: string; assignments: number }[]; average: number }) {
  const [hover, setHover] = useState<{ x: number; y: number; text: string } | null>(null)
  const sorted = [...data].sort((a, b) => a.assignments - b.assignments || a.name.localeCompare(b.name))
  if (sorted.length === 0) return <Empty icon={<IconChart />} title="No TAs yet" />
  const W = 960, H = 300, L = 40, R = 16, T = 16, B = 34
  const maxY = Math.max(4, Math.ceil(Math.max(...sorted.map((d) => d.assignments), average) / 4) * 4)
  const x = (i: number) => L + (sorted.length === 1 ? (W - L - R) / 2 : (i * (W - L - R)) / (sorted.length - 1))
  const y = (v: number) => T + (H - T - B) * (1 - v / maxY)
  const ticks = Array.from({ length: 5 }, (_, i) => (maxY / 4) * i)
  return (
    <div style={{ position: 'relative' }}>
      <svg className="chart" viewBox={`0 0 ${W} ${H}`} role="img" aria-label={`Sessions per TA, average ${average.toFixed(2)}`}>
        {ticks.map((t) => (
          <g key={t}>
            <line className="gridline" x1={L} x2={W - R} y1={y(t)} y2={y(t)} />
            <text x={L - 8} y={y(t) + 4} textAnchor="end">{t}</text>
          </g>
        ))}
        <line className="avg" x1={L} x2={W - R} y1={y(average)} y2={y(average)} />
        <text className="avg-label" x={W - R} y={y(average) - 6} textAnchor="end">average {average.toFixed(2)}</text>
        <text x={(L + W - R) / 2} y={H - 6} textAnchor="middle">TAs, fewest sessions to most</text>
        {sorted.map((d, i) => (
          <circle
            key={d.userId}
            cx={x(i)}
            cy={y(d.assignments)}
            r={5.5}
            className={d.assignments === 0 ? 'dot-zero' : d.assignments >= average ? 'dot-above' : 'dot-below'}
            onMouseEnter={() => setHover({ x: (x(i) / W) * 100, y: (y(d.assignments) / H) * 100, text: `${d.name}: ${d.assignments}` })}
            onMouseLeave={() => setHover(null)}
          >
            <title>{`${d.name}: ${d.assignments}`}</title>
          </circle>
        ))}
      </svg>
      {hover && <div className="chart-tip" style={{ left: `${hover.x}%`, top: `${hover.y}%` }}>{hover.text}</div>}
    </div>
  )
}

function WeekChart({ weeks, currentWeek }: { weeks: Analytics['weeks']; currentWeek: number }) {
  if (weeks.length === 0) return <Empty icon={<IconChart />} title="No quizzes yet" />
  const W = 560, H = 240, L = 30, B = 30, T = 12
  const step = Math.max(1, Math.ceil(Math.max(4, ...weeks.map((w) => w.seatsNeeded)) / 4))
  const max = step * 4
  const band = (W - L) / weeks.length
  const bw = Math.min(28, band * 0.6)
  const y = (v: number) => T + (H - T - B) * (1 - v / max)
  return (
    <svg className="chart" viewBox={`0 0 ${W} ${H}`} role="img" aria-label="Proctor seats needed and filled per week">
      {[0, step, step * 2, step * 3, max].map((t) => (
        <g key={t}>
          <line className="gridline" x1={L} x2={W} y1={y(t)} y2={y(t)} />
          <text x={L - 6} y={y(t) + 4} textAnchor="end">{t}</text>
        </g>
      ))}
      {weeks.map((w, i) => {
        const cx0 = L + band * i + band / 2
        return (
          <g key={w.week}>
            {w.week === currentWeek && <rect x={cx0 - band / 2 + 2} y={T - 4} width={band - 4} height={H - T - B + 4} fill="#F4D35E" opacity={0.35} rx={4} />}
            <rect x={cx0 - bw / 2} y={y(w.seatsNeeded)} width={bw} height={H - B - y(w.seatsNeeded)} style={{ fill: 'var(--line)' }} rx={3} />
            <rect x={cx0 - bw / 2} y={y(w.seatsFilled)} width={bw} height={H - B - y(w.seatsFilled)} style={{ fill: w.seatsFilled < w.seatsNeeded ? 'var(--short)' : 'var(--ink-2)' }} rx={3}>
              <title>{`Week ${w.week}: ${w.quizzes} quizzes, ${w.seatsFilled} of ${w.seatsNeeded} seats filled`}</title>
            </rect>
            <text x={cx0} y={H - 10} textAnchor="middle">{w.week}</text>
          </g>
        )
      })}
    </svg>
  )
}
