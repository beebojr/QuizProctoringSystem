import { useMemo, useRef, useState } from 'react'
import type { DragEvent } from 'react'
import toast from 'react-hot-toast'
import { Link } from 'react-router-dom'
import { api, download, errorMessage, unwrap } from '../../api/client'
import type { ExamType, ImportCommitResult, ImportPreview, ImportQuizPreview, Result } from '../../api/types'
import { PageHead } from '../../components/AppShell'
import { IconAlert, IconCheck, IconDownload, IconFile, IconPlus, IconUpload, IconX } from '../../components/Icons'
import { announceChange } from '../../components/NotificationBell'
import { Spinner, Toggle, cx } from '../../components/ui'
import { useTerm } from '../../contexts/TermContext'
import { readPdfCalendar } from '../../lib/pdfCalendar'
import { hhmm, isFriday, longDate, minutes, plural } from '../../lib/format'

interface EditLoc {
  roomName: string
  group: string
  proctorsNeeded: number
  roomKnown: boolean
  alreadyInQuiz: boolean
}

interface EditQuiz {
  key: string
  include: boolean
  courseName: string
  isNewCourse: boolean
  examType: ExamType
  date: string
  startTime: string
  endTime: string
  weekNumber: number
  existing: boolean
  warnings: string[]
  sourceRows: number[]
  locations: EditLoc[]
}

const toEdit = (q: ImportQuizPreview): EditQuiz => ({
  key: q.key,
  include: true,
  courseName: q.courseName,
  isNewCourse: q.isNewCourse,
  examType: q.examType,
  date: q.date,
  startTime: hhmm(q.startTime),
  endTime: hhmm(q.endTime),
  weekNumber: q.fileWeek ?? q.weekNumber,
  existing: !!q.existingQuizId,
  warnings: q.warnings,
  sourceRows: q.sourceRows,
  locations: q.locations.map((l) => ({ roomName: l.roomName, group: l.group ?? '', proctorsNeeded: l.proctorsNeeded, roomKnown: l.roomKnown, alreadyInQuiz: l.alreadyInQuiz })),
})

export default function ImportCalendar() {
  const { semester, semesterId, settings } = useTerm()
  const input = useRef<HTMLInputElement>(null)
  const [over, setOver] = useState(false)
  const [file, setFile] = useState<File | null>(null)
  const [reading, setReading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [preview, setPreview] = useState<ImportPreview | null>(null)
  const [quizzes, setQuizzes] = useState<EditQuiz[]>([])
  const [createCourses, setCreateCourses] = useState(true)
  const [addReserve, setAddReserve] = useState(false)
  const [autoAssign, setAutoAssign] = useState(true)
  const [committing, setCommitting] = useState(false)
  const [result, setResult] = useState<ImportCommitResult | null>(null)
  const [showIssues, setShowIssues] = useState(false)

  const read = async (f: File) => {
    setFile(f)
    setError(null)
    setPreview(null)
    setResult(null)
    setReading(true)
    try {
      const ext = f.name.toLowerCase().split('.').pop()
      let data: Result<ImportPreview>
      if (ext === 'pdf') {
        const rows = await readPdfCalendar(f)
        if (rows.length === 0) throw new Error('No calendar table was found in this PDF. It needs the WEEK, DATE, SUBJECT, SLOT and LOCATION headers.')
        data = (await api.post<Result<ImportPreview>>('/quizzes/import/preview-rows', { semesterId, rows })).data
      } else if (ext === 'xlsx' || ext === 'xlsm') {
        const form = new FormData()
        form.append('file', f)
        if (semesterId) form.append('semesterId', semesterId)
        data = (await api.post<Result<ImportPreview>>('/quizzes/import/preview', form)).data
      } else {
        throw new Error('Choose the quiz calendar as an Excel workbook (.xlsx) or a PDF.')
      }
      const p = unwrap(data)
      setPreview(p)
      setQuizzes(p.quizzes.map(toEdit))
      setShowIssues(p.issues.some((i) => i.severity === 'Error'))
    } catch (e) {
      setError(errorMessage(e))
    } finally {
      setReading(false)
    }
  }

  const onDrop = (e: DragEvent) => {
    e.preventDefault()
    setOver(false)
    const f = e.dataTransfer.files?.[0]
    if (f) void read(f)
  }

  const patch = (key: string, p: Partial<EditQuiz>) => setQuizzes((list) => list.map((q) => (q.key === key ? { ...q, ...p } : q)))
  const patchLoc = (key: string, i: number, p: Partial<EditLoc>) =>
    setQuizzes((list) => list.map((q) => (q.key === key ? { ...q, locations: q.locations.map((l, j) => (j === i ? { ...l, ...p } : l)) } : q)))

  const problems = useMemo(() => {
    const map = new Map<string, string>()
    for (const q of quizzes) {
      if (!q.include) continue
      if (!q.courseName.trim()) map.set(q.key, 'Course name is empty.')
      else if (!q.date) map.set(q.key, 'Date is missing.')
      else if (isFriday(q.date)) map.set(q.key, 'Falls on a Friday.')
      else if (!q.startTime || !q.endTime || minutes(q.endTime) <= minutes(q.startTime)) map.set(q.key, 'Check the start and end time.')
      else if (q.locations.length === 0 || q.locations.some((l) => !l.roomName.trim())) map.set(q.key, 'Every quiz needs at least one named room.')
      else if (semester && (q.date < semester.startDate || q.date > semester.endDate)) map.set(q.key, `Outside ${semester.name}.`)
    }
    return map
  }, [quizzes, semester])

  const included = quizzes.filter((q) => q.include)
  const seats = included.reduce((n, q) => n + q.locations.reduce((m, l) => m + l.proctorsNeeded, 0) + (addReserve ? settings?.reserveProctors ?? 0 : 0), 0)
  const newCourses = [...new Set(included.filter((q) => q.isNewCourse).map((q) => q.courseName.trim()))]

  const commit = async () => {
    if (problems.size > 0) {
      toast.error('Fix or untick the quizzes marked in red first.')
      document.getElementById(`imp-${[...problems.keys()][0]}`)?.scrollIntoView({ block: 'center', behavior: 'smooth' })
      return
    }
    setCommitting(true)
    try {
      const body = {
        semesterId: preview?.semesterId ?? semesterId,
        createMissingCourses: createCourses,
        addReserve,
        autoAssign,
        quizzes: included.map((q) => ({
          courseName: q.courseName.trim(),
          examType: q.examType,
          date: q.date,
          startTime: q.startTime,
          endTime: q.endTime,
          weekNumber: q.weekNumber || null,
          locations: q.locations.map((l) => ({ roomName: l.roomName.trim(), group: l.group.trim() || null, proctorsNeeded: l.proctorsNeeded })),
        })),
      }
      const { data } = await api.post<Result<ImportCommitResult>>('/quizzes/import/commit', body)
      const r = unwrap(data)
      setResult(r)
      announceChange()
      window.scrollTo({ top: 0, behavior: 'smooth' })
    } catch (e) {
      toast.error(errorMessage(e))
    } finally {
      setCommitting(false)
    }
  }

  const reset = () => {
    setFile(null)
    setPreview(null)
    setQuizzes([])
    setResult(null)
    setError(null)
    if (input.current) input.current.value = ''
  }

  return (
    <>
      <PageHead
        title="Import the quiz calendar"
        sub="Upload the official calendar as Excel or PDF. Merged cells are read like the printed sheet: one quiz per course, date and slot, one room per location."
      >
        <button className="btn" onClick={() => download('/quizzes/import/template', undefined, 'quiz-calendar-template.xlsx').catch((e) => toast.error(errorMessage(e)))}>
          <IconDownload />
          Excel template
        </button>
      </PageHead>

      {result && (
        <section className="sheet sheet-pad stack" style={{ marginBottom: 18 }}>
          <div className="row">
            <IconCheck width={22} height={22} style={{ color: 'var(--ok)' }} />
            <h2>Calendar imported</h2>
          </div>
          <p>
            {plural(result.quizzesCreated, 'quiz', 'quizzes')} added
            {result.quizzesUpdated > 0 ? `, ${plural(result.quizzesUpdated, 'quiz', 'quizzes')} updated` : ''}
            {result.roomsAdded > 0 ? `, ${plural(result.roomsAdded, 'new room')}` : ''}
            {result.coursesCreated.length > 0 ? `, new courses: ${result.coursesCreated.join(', ')}` : ''}.
          </p>
          {result.autoAssign && (
            <div className={cx('notice', result.autoAssign.missing > 0 ? 'warn' : 'ok')}>
              {plural(result.autoAssign.assigned, 'proctor')} assigned automatically.
              {result.autoAssign.missing > 0 && ` ${plural(result.autoAssign.missing, 'seat')} stayed open because nobody passed every rule; fill them from the quiz board or the assignment run.`}
            </div>
          )}
          {result.errors.length > 0 && (
            <div className="notice short">
              <IconAlert />
              <ul>{result.errors.map((e) => <li key={e}>{e}</li>)}</ul>
            </div>
          )}
          <div className="row">
            <Link className="btn primary" to="/quizzes">Open the quiz board</Link>
            {result.autoAssign && result.autoAssign.missing > 0 && <Link className="btn" to="/assign">Assignment run</Link>}
            <button className="btn ghost" onClick={reset}>Import another file</button>
          </div>
        </section>
      )}

      {!preview && !result && (
        <div className="stack">
          <label
            className={cx('drop', over && 'over')}
            onDragOver={(e) => { e.preventDefault(); setOver(true) }}
            onDragLeave={() => setOver(false)}
            onDrop={onDrop}
          >
            {reading ? <Spinner /> : <IconUpload />}
            <b>{reading ? `Reading ${file?.name}…` : 'Drop the calendar here or choose a file'}</b>
            <span className="muted">Excel (.xlsx) or PDF, into {semester?.name ?? 'the active semester'}. Nothing is saved until you confirm.</span>
            <input ref={input} type="file" className="sr-only" accept=".xlsx,.xlsm,.pdf" disabled={reading} onChange={(e) => e.target.files?.[0] && read(e.target.files[0])} />
          </label>
          {error && <div className="notice short" role="alert"><IconAlert /><span>{error}</span></div>}
          <div className="notice">
            <IconFile />
            <span>
              Expected columns: <b>Week</b>, <b>Date</b>, <b>Subject</b>, <b>Slot</b> (1st–5th or Gap), <b>GP</b> and <b>Location</b>. Locations like “C.101-C.102-C.201” become separate rooms.
              Banner rows such as holidays are skipped. Optional columns: Start, End, Proctors, Type.
            </span>
          </div>
        </div>
      )}

      {preview && !result && (
        <div className="stack">
          <section className="sheet sheet-pad spread" style={{ flexWrap: 'wrap' }}>
            <div>
              <h2>{file?.name}</h2>
              <span className="muted small">
                {preview.sheetName ? `Sheet “${preview.sheetName}”, ` : ''}
                {plural(preview.rowsRead, 'row')} read, {plural(preview.quizzes.length, 'quiz', 'quizzes')} found for {preview.semesterName}.
              </span>
            </div>
            <div className="row">
              {preview.issues.length > 0 && (
                <button className="btn small" onClick={() => setShowIssues((s) => !s)}>
                  <IconAlert />
                  {plural(preview.issues.length, 'note')}
                </button>
              )}
              <button className="btn small ghost" onClick={reset}>
                <IconX />
                Choose another file
              </button>
            </div>
          </section>

          {showIssues && preview.issues.length > 0 && (
            <section className="sheet">
              <div className="table-wrap" style={{ maxHeight: 280 }}>
                <table className="grid">
                  <tbody>
                    {preview.issues.map((i, n) => (
                      <tr key={n}>
                        <td className="nowrap muted">{i.row > 0 ? `Row ${i.row}` : 'File'}</td>
                        <td><span className={cx('tag', i.severity === 'Error' ? 'short' : i.severity === 'Skipped' ? 'warn' : '')}>{i.severity}</span></td>
                        <td>{i.message}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>
          )}

          <section className="sheet">
            <div className="sheet-head">
              <h3>Check the quizzes</h3>
              <div className="row">
                <button className="btn ghost small" onClick={() => setQuizzes((l) => l.map((q) => ({ ...q, include: true })))}>Tick all</button>
                <button className="btn ghost small" onClick={() => setQuizzes((l) => l.map((q) => ({ ...q, include: false })))}>Untick all</button>
              </div>
            </div>
            {quizzes.length === 0 && <div className="empty">No quiz rows were recognised. Check the notes above.</div>}
            {quizzes.map((q) => (
              <div key={q.key} id={`imp-${q.key}`} className={cx('import-quiz', !q.include && 'off')}>
                <label className="check" style={{ alignSelf: 'start', marginTop: 26 }}>
                  <input type="checkbox" checked={q.include} onChange={(e) => patch(q.key, { include: e.target.checked })} aria-label={`Import ${q.courseName}`} />
                </label>
                <div style={{ minWidth: 0 }}>
                  <div className="import-fields">
                    <label className="field">
                      <span>
                        Course {q.isNewCourse && <span className="tag hl" style={{ height: 18 }}>New</span>} {q.existing && <span className="tag line" style={{ height: 18 }}>Already imported</span>}
                      </span>
                      <input className="input small" value={q.courseName} onChange={(e) => patch(q.key, { courseName: e.target.value })} />
                    </label>
                    <label className="field">
                      <span>Type</span>
                      <select className="input small" value={q.examType} onChange={(e) => patch(q.key, { examType: e.target.value as ExamType })}>
                        <option>Quiz</option>
                        <option>Midterm</option>
                        <option>Final</option>
                      </select>
                    </label>
                    <label className="field">
                      <span>Date</span>
                      <input className="input small" type="date" value={q.date} onChange={(e) => patch(q.key, { date: e.target.value })} />
                    </label>
                    <label className="field">
                      <span>Starts</span>
                      <input className="input small" type="time" value={q.startTime} onChange={(e) => patch(q.key, { startTime: e.target.value })} />
                    </label>
                    <label className="field">
                      <span>Ends</span>
                      <input className="input small" type="time" value={q.endTime} onChange={(e) => patch(q.key, { endTime: e.target.value })} />
                    </label>
                    <label className="field">
                      <span>Week</span>
                      <input className="input small" type="number" min={1} max={30} value={q.weekNumber || ''} onChange={(e) => patch(q.key, { weekNumber: Number(e.target.value) })} />
                    </label>
                  </div>
                  <div className="import-locs">
                    {q.locations.map((l, i) => (
                      <span key={i} className={cx('import-loc', !l.roomKnown && 'new-room')} title={l.roomKnown ? undefined : 'New room, added to the room list'}>
                        <input className="r room" value={l.roomName} aria-label="Room" onChange={(e) => patchLoc(q.key, i, { roomName: e.target.value })} />
                        <input className="g" value={l.group} placeholder="group" aria-label="Group" onChange={(e) => patchLoc(q.key, i, { group: e.target.value })} />
                        <input className="n" type="number" min={1} max={10} value={l.proctorsNeeded} aria-label="Proctors needed" onChange={(e) => patchLoc(q.key, i, { proctorsNeeded: Math.max(1, Number(e.target.value) || 1) })} />
                        <button className="icon-btn" style={{ width: 24, height: 24 }} aria-label="Remove room" onClick={() => patch(q.key, { locations: q.locations.filter((_, j) => j !== i) })}>
                          <IconX width={13} height={13} />
                        </button>
                      </span>
                    ))}
                    <button className="btn small ghost" onClick={() => patch(q.key, { locations: [...q.locations, { roomName: '', group: '', proctorsNeeded: settings?.defaultProctorsPerRoom ?? 2, roomKnown: true, alreadyInQuiz: false }] })}>
                      <IconPlus />
                      Room
                    </button>
                  </div>
                  <div className="small muted" style={{ marginTop: 6 }}>
                    {q.date ? longDate(q.date) : 'No date'}. From row{q.sourceRows.length > 1 ? 's' : ''} {q.sourceRows.join(', ')}.
                  </div>
                  {(problems.get(q.key) || q.warnings.length > 0) && (
                    <div className="small" style={{ marginTop: 4 }}>
                      {problems.get(q.key) && <div style={{ color: 'var(--short)', fontWeight: 600 }}>{problems.get(q.key)}</div>}
                      {q.warnings.map((w) => <div key={w} style={{ color: 'var(--warn)' }}>{w}</div>)}
                    </div>
                  )}
                </div>
              </div>
            ))}
          </section>

          <section className="sheet sheet-pad stack" style={{ position: 'sticky', bottom: 12, boxShadow: 'var(--shadow-pop)' }}>
            <div className="row-wrap" style={{ gap: 22 }}>
              <Toggle checked={createCourses} onChange={setCreateCourses} label="Add new courses" hint={newCourses.length ? `${newCourses.length} not in the list yet` : 'All courses are known'} />
              <Toggle checked={addReserve} onChange={setAddReserve} label="Add a reserve to each quiz" hint={`${settings?.reserveProctors ?? 2} standby proctors`} />
              <Toggle checked={autoAssign} onChange={setAutoAssign} label="Assign proctors now" hint="Fills seats with the scheduling rules" />
              <span className="grow" />
              <button className="btn mark" onClick={commit} disabled={committing || included.length === 0}>
                {committing ? 'Importing…' : `Import ${plural(included.length, 'quiz', 'quizzes')} (${seats} seats)`}
              </button>
            </div>
            {!createCourses && newCourses.length > 0 && <p className="small" style={{ color: 'var(--warn)' }}>Quizzes of new courses ({newCourses.join(', ')}) will be skipped.</p>}
          </section>
        </div>
      )}
    </>
  )
}
