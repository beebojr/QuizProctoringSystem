import { useEffect, useMemo, useState } from 'react'
import toast from 'react-hot-toast'
import { useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { errorMessage, get, send } from '../../api/client'
import type { Course, ExamType, LocationInput, Quiz, QuizSaveResult, Room } from '../../api/types'
import { PageHead } from '../../components/AppShell'
import { Calendar } from '../../components/Calendar'
import { IconAlert, IconPlus, IconTrash } from '../../components/Icons'
import { announceChange } from '../../components/NotificationBell'
import { Combobox, ErrorNote, Field, Loading, Segmented, Toggle, cx } from '../../components/ui'
import { useTerm } from '../../contexts/TermContext'
import { useQuery } from '../../lib/hooks'
import { SLOTS, academicWeek, hhmm, isFriday, longDate, minutes, plural, slotForTimes, timeRange } from '../../lib/format'

interface Row extends LocationInput {
  key: string
}

let keySeq = 0
const newKey = () => `r${++keySeq}`

export default function QuizEditor() {
  const { id } = useParams()
  const [params] = useSearchParams()
  const fromId = params.get('from')
  const editing = !!id
  const navigate = useNavigate()
  const { semester, settings } = useTerm()

  const courses = useQuery(() => get<Course[]>('/courses'), [])
  const rooms = useQuery(() => get<Room[]>('/rooms'), [])
  const semesterQuizzes = useQuery(() => get<Quiz[]>('/quizzes', { semesterId: semester?.id }), [semester?.id], !!semester)
  const source = useQuery(() => get<Quiz>(`/quizzes/${id ?? fromId}`), [id, fromId], !!(id || fromId))

  const defaultProctors = settings?.defaultProctorsPerRoom ?? 2
  const [courseId, setCourseId] = useState<string | null>(null)
  const [courseText, setCourseText] = useState('')
  const [examType, setExamType] = useState<ExamType>('Quiz')
  const [date, setDate] = useState<string | null>(null)
  const [start, setStart] = useState('')
  const [end, setEnd] = useState('')
  const [weekOverride, setWeekOverride] = useState<number | null>(null)
  const [rows, setRows] = useState<Row[]>(() => [{ key: newKey(), roomName: '', group: '', proctorsNeeded: 2 }])
  const [autoAssign, setAutoAssign] = useState(true)
  const [hasReserve, setHasReserve] = useState(false)
  const [notes, setNotes] = useState('')
  const [saving, setSaving] = useState(false)
  const [tried, setTried] = useState(false)
  const [serverError, setServerError] = useState<string | null>(null)

  // New quiz: start with the configured number of proctors per room.
  useEffect(() => {
    if (!editing && !fromId && settings) setRows((r) => r.map((x) => (x.roomName ? x : { ...x, proctorsNeeded: settings.defaultProctorsPerRoom })))
  }, [settings, editing, fromId])

  // Edit or duplicate: copy the source quiz into the form once.
  useEffect(() => {
    const q = source.data
    if (!q) return
    setCourseId(q.courseId)
    setCourseText(q.courseName)
    setExamType(q.examType)
    setStart(hhmm(q.startTime))
    setEnd(hhmm(q.endTime))
    setNotes(q.notes ?? '')
    setAutoAssign(editing ? false : q.autoAssign)
    const reserve = q.locations.find((l) => l.isReserve)
    if (editing) {
      setDate(q.quizDate)
      setWeekOverride(q.weekNumber)
      setRows(q.locations.map((l) => ({ key: newKey(), id: l.id, roomName: l.roomName, group: l.group ?? '', proctorsNeeded: l.proctorsNeeded, isReserve: l.isReserve })))
    } else {
      setDate(null)
      setHasReserve(!!reserve)
      setRows(q.locations.filter((l) => !l.isReserve).map((l) => ({ key: newKey(), roomName: l.roomName, group: l.group ?? '', proctorsNeeded: l.proctorsNeeded })))
    }
  }, [source.data, editing])

  const computedWeek = date && semester ? academicWeek(semester.startDate, date) : null
  const week = weekOverride ?? computedWeek
  const slot = start && end ? slotForTimes(start, end) : undefined
  const sameDay = useMemo(() => (semesterQuizzes.data ?? []).filter((q) => q.quizDate === date && q.id !== id), [semesterQuizzes.data, date, id])
  const marked = useMemo(() => new Set((semesterQuizzes.data ?? []).map((q) => q.quizDate)), [semesterQuizzes.data])
  const roomByName = useMemo(() => new Map((rooms.data ?? []).map((r) => [r.name.toLowerCase(), r])), [rooms.data])

  // Rooms already used by another quiz at an overlapping time that day.
  const busyRooms = useMemo(() => {
    const set = new Set<string>()
    if (!start || !end) return set
    for (const q of sameDay) {
      if (minutes(q.startTime) < minutes(end) && minutes(q.endTime) > minutes(start))
        q.locations.filter((l) => !l.isReserve).forEach((l) => set.add(l.roomName.toLowerCase()))
    }
    return set
  }, [sameDay, start, end])

  const regularRows = rows.filter((r) => !r.isReserve)
  const reserveRow = rows.find((r) => r.isReserve)
  const errors = {
    course: !courseId && !courseText.trim() ? 'Choose a course.' : null,
    date: !date ? 'Pick the quiz date.' : isFriday(date) ? 'Friday is a day off.' : null,
    time: !start || !end ? 'Pick a slot or enter both times.' : minutes(end) <= minutes(start) ? 'The end time must be after the start time.' : null,
    rooms: regularRows.length === 0
      ? 'Add at least one room.'
      : regularRows.some((r) => !r.roomName.trim())
        ? 'Every room needs a name.'
        : new Set(regularRows.map((r) => r.roomName.trim().toLowerCase())).size !== regularRows.length
          ? 'A room is listed twice.'
          : null,
  }
  const valid = !Object.values(errors).some(Boolean)

  const setRow = (key: string, patch: Partial<Row>) => setRows((rs) => rs.map((r) => (r.key === key ? { ...r, ...patch } : r)))

  const ensureCourse = async (): Promise<string> => {
    if (courseId) return courseId
    const name = courseText.trim()
    const existing = courses.data?.find((c) => c.name.toLowerCase() === name.toLowerCase())
    if (existing) return existing.id
    const created = await send<Course>('post', '/courses', { name })
    courses.setData((list) => [...(list ?? []), created])
    setCourseId(created.id)
    toast.success(`Course “${created.name}” added.`)
    return created.id
  }

  const save = async () => {
    setTried(true)
    setServerError(null)
    if (!valid || !date) return
    setSaving(true)
    try {
      const cid = await ensureCourse()
      const locations: LocationInput[] = rows.map((r) => ({
        id: r.id ?? null,
        roomName: r.isReserve ? r.roomName || 'Reserved' : r.roomName.trim(),
        group: r.group?.trim() || null,
        proctorsNeeded: r.proctorsNeeded,
        isReserve: !!r.isReserve,
      }))
      const body = {
        courseId: cid,
        semesterId: semester?.id,
        examType,
        quizDate: date,
        startTime: start,
        endTime: end,
        notes: notes.trim() || null,
        autoAssign,
        hasReserve,
        locations,
        weekNumber: weekOverride !== null && weekOverride !== computedWeek ? weekOverride : null,
      }
      const result = editing
        ? await send<QuizSaveResult>('put', `/quizzes/${id}`, body)
        : await send<QuizSaveResult>('post', '/quizzes', body)
      toast.success(editing ? 'Quiz saved.' : 'Quiz added.')
      result.warnings.forEach((w) => toast(w, { icon: '⚠️', duration: 7000 }))
      const aa = result.autoAssign
      if (aa && aa.missing > 0) toast(`${plural(aa.missing, 'seat')} could not be filled automatically. Pick proctors by hand on the quiz.`, { icon: '⚠️', duration: 8000 })
      else if (aa && aa.assigned > 0) toast.success(`${plural(aa.assigned, 'proctor')} assigned automatically.`)
      announceChange()
      navigate(`/quizzes?focus=${result.quiz.id}`)
    } catch (e) {
      setServerError(errorMessage(e))
    } finally {
      setSaving(false)
    }
  }

  if ((id || fromId) && source.loading && !source.data) return <Loading />
  if (source.error) return <ErrorNote message={source.error} onRetry={source.reload} />

  const showErr = (k: keyof typeof errors) => (tried ? errors[k] : null)

  return (
    <>
      <PageHead
        title={editing ? 'Edit quiz' : 'Add a quiz'}
        sub={editing ? 'Changing the date or time re-checks every assigned proctor against the rules.' : 'Pick the course, the slot and the rooms. Proctors can be assigned now or later.'}
      />
      <div className="overview">
        <div className="stack">
          <section className="sheet sheet-pad stack">
            <h2>Course</h2>
            <div className="form-grid">
              <Field label="Course" error={showErr('course')} className="span-2">
                <Combobox
                  value={courseText}
                  placeholder="Search courses or type a new one"
                  options={(courses.data ?? []).map((c) => ({ id: c.id, label: c.name, hint: c.code ?? undefined }))}
                  onPick={(o, text) => {
                    setCourseId(o?.id ?? null)
                    setCourseText(text)
                  }}
                  onCreate={(text) => {
                    setCourseId(null)
                    setCourseText(text)
                  }}
                  createLabel="Use new course"
                />
              </Field>
              <Field label="Type">
                <Segmented value={examType} onChange={setExamType} options={(['Quiz', 'Midterm', 'Final'] as ExamType[]).map((t) => ({ value: t, label: t }))} label="Exam type" />
              </Field>
            </div>
            {!courseId && courseText.trim().length > 1 && <p className="small muted">“{courseText.trim()}” is not in the course list yet. It is added when you save.</p>}
          </section>

          <section className="sheet sheet-pad stack">
            <div className="spread">
              <h2>When</h2>
              {date && (
                <span className="muted">
                  {longDate(date)}
                  {week ? `, week ${week}` : ''}
                </span>
              )}
            </div>
            {showErr('date') && <p className="error-text small" style={{ color: 'var(--short)' }}>{showErr('date')}</p>}
            <div className="form-grid">
              <Calendar
                value={date}
                onChange={(d) => {
                  setDate(d)
                  setWeekOverride(null)
                }}
                min={semester?.startDate}
                max={semester?.endDate}
                marked={marked}
              />
              <div className="stack">
                <Field label="Slot">
                  <div className="slot-picks" style={{ gridTemplateColumns: 'repeat(3, minmax(0, 1fr))' }}>
                    {SLOTS.map((s) => (
                      <button
                        type="button"
                        key={s.number}
                        className={cx('slot-pick', slot?.number === s.number && 'on')}
                        aria-pressed={slot?.number === s.number}
                        onClick={() => {
                          setStart(s.start)
                          setEnd(s.end)
                        }}
                      >
                        <b>{s.label}</b>
                        <span>{timeRange(s.start, s.end)}</span>
                      </button>
                    ))}
                  </div>
                </Field>
                <div className="form-grid">
                  <Field label="Starts" error={showErr('time')}>
                    <input className="input" type="time" value={start} onChange={(e) => setStart(e.target.value)} />
                  </Field>
                  <Field label="Ends">
                    <input className="input" type="time" value={end} onChange={(e) => setEnd(e.target.value)} />
                  </Field>
                </div>
                <Field label="Week label" hint={computedWeek ? `Week ${computedWeek} counted from the semester start. Change it only if the official calendar says otherwise.` : 'Set from the date.'}>
                  <input className="input" type="number" min={1} max={30} value={week ?? ''} disabled={!date} onChange={(e) => setWeekOverride(e.target.value ? Number(e.target.value) : null)} />
                </Field>
              </div>
            </div>
          </section>

          <section className="sheet sheet-pad stack">
            <div className="spread">
              <h2>Rooms</h2>
              <span className="muted">
                {regularRows.reduce((n, r) => n + r.proctorsNeeded, 0) + (reserveRow?.proctorsNeeded ?? (hasReserve ? settings?.reserveProctors ?? 0 : 0))} proctors in total
              </span>
            </div>
            {showErr('rooms') && <p className="small" style={{ color: 'var(--short)' }}>{showErr('rooms')}</p>}
            <datalist id="room-names">
              {(rooms.data ?? []).map((r) => (
                <option key={r.id} value={r.name} />
              ))}
            </datalist>
            <div className="loc-rows">
              {rows.map((r) => (
                <div key={r.key} className={cx('loc-row', r.isReserve && 'reserve')}>
                  <Field label={r.isReserve ? 'Reserve' : 'Room'} hint={!r.isReserve && busyRooms.has(r.roomName.trim().toLowerCase()) ? <span style={{ color: 'var(--short)' }}>Another quiz uses this room then.</span> : undefined}>
                    <input
                      className="input"
                      list={r.isReserve ? undefined : 'room-names'}
                      value={r.roomName}
                      placeholder="e.g. M1.105"
                      disabled={r.isReserve}
                      onChange={(e) => {
                        const known = roomByName.get(e.target.value.trim().toLowerCase())
                        setRow(r.key, { roomName: e.target.value, ...(known && !r.id ? { proctorsNeeded: known.defaultProctors } : {}) })
                      }}
                    />
                  </Field>
                  <Field label="Group">
                    <input className="input" value={r.group ?? ''} placeholder="GPI" disabled={r.isReserve} onChange={(e) => setRow(r.key, { group: e.target.value })} />
                  </Field>
                  <Field label="Proctors">
                    <Segmented value={r.proctorsNeeded} onChange={(n) => setRow(r.key, { proctorsNeeded: n })} options={[1, 2, 3, 4, 5, 6].map((n) => ({ value: n, label: n }))} label="Proctors needed" />
                  </Field>
                  <button type="button" className="icon-btn" aria-label="Remove room" disabled={!r.isReserve && regularRows.length === 1} onClick={() => setRows((rs) => rs.filter((x) => x.key !== r.key))}>
                    <IconTrash />
                  </button>
                </div>
              ))}
            </div>
            <div className="row-wrap">
              <button type="button" className="btn small" onClick={() => setRows((rs) => [...rs.filter((x) => !x.isReserve), { key: newKey(), roomName: '', group: '', proctorsNeeded: defaultProctors }, ...rs.filter((x) => x.isReserve)])}>
                <IconPlus />
                Add another room
              </button>
              {editing && !reserveRow && (
                <button type="button" className="btn small ghost" onClick={() => setRows((rs) => [...rs, { key: newKey(), roomName: 'Reserved', group: '', proctorsNeeded: settings?.reserveProctors ?? 2, isReserve: true }])}>
                  <IconPlus />
                  Add a reserve
                </button>
              )}
            </div>
          </section>
        </div>

        <aside className="stack" style={{ position: 'sticky', top: 64 }}>
          <section className="sheet sheet-pad stack">
            <h2>Proctors</h2>
            <Toggle
              checked={autoAssign}
              onChange={setAutoAssign}
              label={editing ? 'Fill empty seats on save' : 'Assign proctors automatically'}
              hint="Uses the same rules as manual assignment: schedules, days off, excuses, weekly and semester limits."
            />
            {!editing && (
              <Toggle
                checked={hasReserve}
                onChange={setHasReserve}
                label="Add a reserve"
                hint={`${settings?.reserveProctors ?? 2} extra proctors on standby in case someone can't come.`}
              />
            )}
            <Field label="Notes">
              <textarea className="input" value={notes} onChange={(e) => setNotes(e.target.value)} placeholder="Anything proctors should know" />
            </Field>
          </section>

          {date && (
            <section className="sheet">
              <div className="sheet-head">
                <h3>Also on {longDate(date)}</h3>
              </div>
              <div className="sheet-pad stack-s" style={{ paddingTop: 12 }}>
                {sameDay.length === 0 && <p className="muted small">No other quiz that day.</p>}
                {sameDay.map((q) => (
                  <div key={q.id} className="small">
                    <b>{q.courseName}</b>
                    <div className="muted">
                      {timeRange(q.startTime, q.endTime)}, {q.locations.filter((l) => !l.isReserve).map((l) => l.roomName).join(', ')}
                    </div>
                  </div>
                ))}
              </div>
            </section>
          )}

          {serverError && (
            <div className="notice short" role="alert">
              <IconAlert />
              <span>{serverError}</span>
            </div>
          )}
          {tried && !valid && <div className="notice warn">Fix the highlighted fields to save.</div>}
          <div className="row">
            <button className="btn primary grow" onClick={save} disabled={saving}>
              {saving ? 'Saving…' : editing ? 'Save changes' : 'Add quiz'}
            </button>
            <button className="btn" onClick={() => navigate(-1)}>
              Cancel
            </button>
          </div>
        </aside>
      </div>
    </>
  )
}
