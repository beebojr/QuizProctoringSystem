import { useEffect, useMemo, useState } from 'react'
import toast from 'react-hot-toast'
import { Link, useSearchParams } from 'react-router-dom'
import { download, errorMessage, get } from '../../api/client'
import type { Quiz, QuizStatus } from '../../api/types'
import { PageHead } from '../../components/AppShell'
import { IconCalendar, IconChevronDown, IconDownload, IconPlus, IconUpload } from '../../components/Icons'
import { QuizCard } from '../../components/QuizCard'
import { Empty, ErrorNote, Loading, SearchBox, Segmented, Toggle, cx } from '../../components/ui'
import { useTerm } from '../../contexts/TermContext'
import { useLocalState, useQuery } from '../../lib/hooks'
import { longDate, plural, todayIso } from '../../lib/format'

type Filter = 'All' | QuizStatus

export default function Quizzes() {
  const { semester } = useTerm()
  const [params, setParams] = useSearchParams()
  const [storedFilter, setStoredFilter] = useLocalState<Filter>('qps.quizFilter', 'Upcoming')
  const filter = (params.get('status') as Filter | null) ?? storedFilter
  const incomplete = params.get('incomplete') === '1'
  const focus = params.get('focus')
  const [search, setSearch] = useState('')
  const [folded, setFolded] = useState<Set<string>>(new Set())
  const quizzes = useQuery(() => get<Quiz[]>('/quizzes', { semesterId: semester?.id }), [semester?.id], !!semester)

  const setParam = (key: string, value: string | null) => {
    const next = new URLSearchParams(params)
    if (value === null) next.delete(key)
    else next.set(key, value)
    next.delete('focus')
    setParams(next, { replace: true })
  }

  const visible = useMemo(() => {
    const q = search.trim().toLowerCase()
    return (quizzes.data ?? [])
      .filter((x) => filter === 'All' || x.status === filter)
      .filter((x) => !incomplete || x.proctorsAssigned < x.proctorsNeeded)
      .filter(
        (x) =>
          !q ||
          x.courseName.toLowerCase().includes(q) ||
          x.locations.some(
            (l) => l.roomName.toLowerCase().includes(q) || (l.group ?? '').toLowerCase().includes(q) || l.proctors.some((p) => p.name.toLowerCase().includes(q)),
          ),
      )
      .sort((a, b) => {
        const d = a.quizDate.localeCompare(b.quizDate) || a.startTime.localeCompare(b.startTime)
        return filter === 'Finished' ? -d : d
      })
  }, [quizzes.data, filter, incomplete, search])

  const days = useMemo(() => {
    const map = new Map<string, Quiz[]>()
    for (const q of visible) map.set(q.quizDate, [...(map.get(q.quizDate) ?? []), q])
    return [...map.entries()]
  }, [visible])

  const counts = useMemo(() => {
    const all = quizzes.data ?? []
    return {
      open: all.filter((q) => q.status === 'Upcoming' && q.proctorsAssigned < q.proctorsNeeded).length,
      seats: all.filter((q) => q.status === 'Upcoming').reduce((n, q) => n + Math.max(0, q.proctorsNeeded - q.proctorsAssigned), 0),
    }
  }, [quizzes.data])

  // Scroll to a quiz after saving it.
  useEffect(() => {
    if (!focus || !quizzes.data) return
    const el = document.getElementById(`quiz-${focus}`)
    if (el) {
      el.scrollIntoView({ block: 'center', behavior: 'smooth' })
      el.animate([{ boxShadow: '0 0 0 4px #F4D35E' }, { boxShadow: '0 0 0 0 transparent' }], { duration: 1800, easing: 'ease-out' })
    }
  }, [focus, quizzes.data])

  const replaceQuiz = (q: Quiz) => quizzes.setData((list) => list?.map((x) => (x.id === q.id ? q : x)))
  const removeQuiz = (id: string) => quizzes.setData((list) => list?.filter((x) => x.id !== id))
  const today = todayIso()

  const exportAll = async () => {
    try {
      await download('/quizzes/export', { semesterId: semester?.id }, 'quizzes.xlsx')
    } catch (e) {
      toast.error(errorMessage(e))
    }
  }

  return (
    <>
      <PageHead
        title="Quizzes"
        sub={
          counts.open > 0
            ? `${plural(counts.open, 'upcoming quiz', 'upcoming quizzes')} still ${counts.open === 1 ? 'needs' : 'need'} proctors (${plural(counts.seats, 'open seat')}). Click a dashed seat to fill it.`
            : 'Every upcoming quiz has its proctors. Click a name to replace or remove someone.'
        }
      >
        <button className="btn" onClick={exportAll}>
          <IconDownload />
          Export to Excel
        </button>
        <Link className="btn" to="/import">
          <IconUpload />
          Import calendar
        </Link>
        <Link className="btn primary" to="/quizzes/new">
          <IconPlus />
          Add quiz
        </Link>
      </PageHead>

      <div className="toolbar">
        <SearchBox value={search} onChange={setSearch} placeholder="Course, room, group or proctor" />
        <Segmented
          value={filter}
          onChange={(v) => {
            setStoredFilter(v)
            setParam('status', null)
          }}
          options={(['All', 'Upcoming', 'Started', 'Finished'] as Filter[]).map((v) => ({ value: v, label: v }))}
          label="Status"
          dark
        />
        <Toggle checked={incomplete} onChange={(v) => setParam('incomplete', v ? '1' : null)} label="Missing proctors only" />
        <span className="grow" />
        {days.length > 1 && (
          <button className="btn ghost small" onClick={() => setFolded(folded.size ? new Set() : new Set(days.map(([d]) => d)))}>
            {folded.size ? 'Expand all days' : 'Collapse all days'}
          </button>
        )}
      </div>

      {quizzes.error && <ErrorNote message={quizzes.error} onRetry={quizzes.reload} />}
      {quizzes.loading && !quizzes.data && <Loading />}
      {quizzes.data && days.length === 0 && (
        <div className="sheet">
          <Empty icon={<IconCalendar />} title={quizzes.data.length === 0 ? 'No quizzes in this semester yet' : 'No quiz matches these filters'}>
            {quizzes.data.length === 0 ? (
              <span>
                <Link to="/import">Import the quiz calendar</Link> or <Link to="/quizzes/new">add a quiz</Link>.
              </span>
            ) : (
              'Try another status or clear the search.'
            )}
          </Empty>
        </div>
      )}

      {days.map(([date, list]) => {
        const isFolded = folded.has(date)
        const short = list.filter((q) => q.proctorsAssigned < q.proctorsNeeded).length
        return (
          <section key={date} className="day">
            <div className={cx('day-head', date === today && 'today')}>
              <button
                className={cx('fold', isFolded && 'folded')}
                aria-expanded={!isFolded}
                onClick={() => {
                  const next = new Set(folded)
                  if (isFolded) next.delete(date)
                  else next.add(date)
                  setFolded(next)
                }}
              >
                <IconChevronDown />
                <span className="date">
                  <span>{longDate(date)}</span>
                  <small>
                    {date === today ? 'Today, ' : ''}
                    {plural(list.length, 'quiz', 'quizzes')}
                    {short > 0 ? `, ${short} short` : ''}
                  </small>
                </span>
              </button>
              <span className="rule" />
            </div>
            {!isFolded && (
              <div className="quiz-grid">
                {list.map((q) => (
                  <div key={q.id} id={`quiz-${q.id}`} style={{ borderRadius: 14 }}>
                    <QuizCard quiz={q} onReplace={replaceQuiz} onRemove={removeQuiz} />
                  </div>
                ))}
              </div>
            )}
          </section>
        )
      })}
    </>
  )
}
