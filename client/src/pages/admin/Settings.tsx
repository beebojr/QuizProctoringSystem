import { useEffect, useState } from 'react'
import type { ChangeEvent } from 'react'
import toast from 'react-hot-toast'
import { errorMessage, get, send } from '../../api/client'
import type { Room, Semester, Settings } from '../../api/types'
import { PageHead } from '../../components/AppShell'
import { IconBook, IconCheck, IconEdit, IconPlus, IconTrash, IconX } from '../../components/Icons'
import { ManageCourses } from '../../components/ManageCourses'
import { ErrorNote, Field, Loading, Modal, Toggle, cx, useConfirm } from '../../components/ui'
import { useTerm } from '../../contexts/TermContext'
import { useQuery } from '../../lib/hooks'
import { fullDate } from '../../lib/format'

export default function SettingsPage() {
  const term = useTerm()
  const [courses, setCourses] = useState(false)
  return (
    <>
      <PageHead title="Settings" sub="The rules every assignment follows, the semester calendar and the rooms.">
        <button className="btn" onClick={() => setCourses(true)}>
          <IconBook />
          Manage courses
        </button>
      </PageHead>
      <div className="stack" style={{ gap: 18 }}>
        {term.settings && <RulesForm settings={term.settings} onSaved={term.reload} />}
        <SemestersSection semesters={term.semesters} onChanged={term.reload} />
        <RoomsSection defaultProctors={term.settings?.defaultProctorsPerRoom ?? 2} />
      </div>
      {courses && <ManageCourses onClose={() => setCourses(false)} department={term.settings?.departmentName} />}
    </>
  )
}

function RulesForm({ settings, onSaved }: { settings: Settings; onSaved: () => Promise<void> }) {
  const [form, setForm] = useState(settings)
  const [busy, setBusy] = useState(false)
  useEffect(() => setForm(settings), [settings])
  const dirty = JSON.stringify(form) !== JSON.stringify(settings)
  const num = (k: keyof Settings) => (e: ChangeEvent<HTMLInputElement>) => setForm({ ...form, [k]: e.target.value === '' ? 0 : Number(e.target.value) })

  const save = async () => {
    setBusy(true)
    try {
      await send('put', '/settings', form)
      await onSaved()
      toast.success('Rules saved. They apply to every assignment from now on.')
    } catch (e) {
      toast.error(errorMessage(e))
    } finally {
      setBusy(false)
    }
  }

  return (
    <section className="sheet">
      <div className="sheet-head">
        <h2>Assignment rules</h2>
        <div className="row">
          {dirty && <button className="btn ghost small" onClick={() => setForm(settings)}>Undo changes</button>}
          <button className="btn primary small" onClick={save} disabled={!dirty || busy}>Save rules</button>
        </div>
      </div>
      <div className="sheet-pad">
        <div className="form-grid" style={{ gridTemplateColumns: 'repeat(3, minmax(0, 1fr))' }}>
          <Field label="Sessions per TA per semester" hint="Rule 1. The default limit; each TA can have their own.">
            <input className="input" type="number" min={0} max={200} value={form.defaultMaxSessionsPerSemester} onChange={num('defaultMaxSessionsPerSemester')} />
          </Field>
          <Field label="Sessions per TA per week" hint="Rule 2. Weeks run in 7-day blocks from the semester start.">
            <input className="input" type="number" min={1} max={14} value={form.maxSessionsPerWeek} onChange={num('maxSessionsPerWeek')} />
          </Field>
          <Field label="Target workload (points)" hint="The fair share each TA should reach. One session is one point.">
            <input className="input" type="number" min={0} max={200} step={0.5} value={form.defaultTargetWorkload} onChange={num('defaultTargetWorkload')} />
          </Field>
          <Field label="Proctors per room" hint="Suggested for new rooms and quizzes.">
            <input className="input" type="number" min={1} max={10} value={form.defaultProctorsPerRoom} onChange={num('defaultProctorsPerRoom')} />
          </Field>
          <Field label="Reserve proctors" hint="Standby proctors when a quiz gets a reserve.">
            <input className="input" type="number" min={0} max={10} value={form.reserveProctors} onChange={num('reserveProctors')} />
          </Field>
          <Field label="Department">
            <input className="input" value={form.departmentName} onChange={(e) => setForm({ ...form, departmentName: e.target.value })} />
          </Field>
          <div className="span-2" style={{ gridColumn: '1 / -1' }}>
            <Toggle
              checked={form.preferCourseTas}
              onChange={(v) => setForm({ ...form, preferCourseTas: v })}
              label="Put a TA of the course in every room first"
              hint="Rule 3 (no clash with the teaching schedule), days off, Fridays and excuses always apply."
            />
          </div>
        </div>
      </div>
    </section>
  )
}

function SemestersSection({ semesters, onChanged }: { semesters: Semester[]; onChanged: () => Promise<void> }) {
  const confirm = useConfirm()
  const [editing, setEditing] = useState<Semester | 'new' | null>(null)

  const activate = async (s: Semester) => {
    const ok = await confirm({
      title: `Make ${s.name} the active semester?`,
      body: <p>TAs see and edit the schedule of the active semester, and new quizzes go into it by default.</p>,
      confirm: 'Make active',
    })
    if (!ok) return
    try {
      await send('post', `/semesters/${s.id}/activate`)
      await onChanged()
      toast.success(`${s.name} is now active.`)
    } catch (e) {
      toast.error(errorMessage(e))
    }
  }

  return (
    <section className="sheet">
      <div className="sheet-head">
        <h2>Semesters</h2>
        <button className="btn small" onClick={() => setEditing('new')}>
          <IconPlus />
          New semester
        </button>
      </div>
      <div className="table-wrap">
        <table className="grid">
          <thead>
            <tr>
              <th>Semester</th>
              <th>Starts</th>
              <th>Ends</th>
              <th>Weeks</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {semesters.map((s) => (
              <tr key={s.id}>
                <td>
                  <b style={{ fontWeight: 600 }}>{s.name}</b> {s.isActive && <span className="tag ok">Active</span>}
                </td>
                <td>{fullDate(s.startDate)}</td>
                <td>{fullDate(s.endDate)}</td>
                <td>{s.isActive && s.currentWeek >= 1 && s.currentWeek <= s.totalWeeks ? `Week ${s.currentWeek} of ${s.totalWeeks}` : s.totalWeeks}</td>
                <td className="nowrap" style={{ textAlign: 'right' }}>
                  {!s.isActive && <button className="btn small ghost" onClick={() => activate(s)}>Make active</button>}
                  <button className="icon-btn" aria-label={`Edit ${s.name}`} onClick={() => setEditing(s)}><IconEdit /></button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <p className="small muted sheet-pad" style={{ paddingTop: 10 }}>
        The start date sets week 1. The university calendar counts weeks from the first teaching day, so use that date.
      </p>
      {editing && <SemesterModal semester={editing === 'new' ? undefined : editing} onClose={() => setEditing(null)} onSaved={onChanged} />}
    </section>
  )
}

function SemesterModal({ semester, onClose, onSaved }: { semester?: Semester; onClose: () => void; onSaved: () => Promise<void> }) {
  const [name, setName] = useState(semester?.name ?? '')
  const [start, setStart] = useState(semester?.startDate ?? '')
  const [end, setEnd] = useState(semester?.endDate ?? '')
  const [makeActive, setMakeActive] = useState(!semester)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const save = async () => {
    setError(null)
    if (!name.trim() || !start || !end) return setError('Name, start and end are required.')
    if (end <= start) return setError('The semester must end after it starts.')
    setBusy(true)
    try {
      if (semester) await send('put', `/semesters/${semester.id}`, { name: name.trim(), startDate: start, endDate: end })
      else await send('post', '/semesters', { name: name.trim(), startDate: start, endDate: end, makeActive })
      await onSaved()
      toast.success(semester ? 'Semester saved.' : 'Semester created.')
      onClose()
    } catch (e) {
      setError(errorMessage(e))
    } finally {
      setBusy(false)
    }
  }
  return (
    <Modal
      title={semester ? `Edit ${semester.name}` : 'New semester'}
      subtitle={semester ? 'Changing the start date renumbers the academic weeks used by the weekly rule.' : undefined}
      onClose={onClose}
      footer={
        <>
          <button className="btn" onClick={onClose}>Cancel</button>
          <button className="btn primary" onClick={save} disabled={busy}>{semester ? 'Save semester' : 'Create semester'}</button>
        </>
      }
    >
      <div className="form-grid">
        <Field label="Name" className="span-2">
          <input className="input" value={name} placeholder="Spring 2027" onChange={(e) => setName(e.target.value)} />
        </Field>
        <Field label="First teaching day">
          <input className="input" type="date" value={start} onChange={(e) => setStart(e.target.value)} />
        </Field>
        <Field label="Last day">
          <input className="input" type="date" value={end} min={start} onChange={(e) => setEnd(e.target.value)} />
        </Field>
        {!semester && (
          <div className="span-2">
            <Toggle checked={makeActive} onChange={setMakeActive} label="Make it the active semester" />
          </div>
        )}
        {error && <div className="notice short span-2">{error}</div>}
      </div>
    </Modal>
  )
}

function RoomsSection({ defaultProctors }: { defaultProctors: number }) {
  const rooms = useQuery(() => get<Room[]>('/rooms'), [])
  const confirm = useConfirm()
  const [edit, setEdit] = useState<{ id?: string; name: string; capacity: string; defaultProctors: number } | null>(null)

  const save = async () => {
    if (!edit || !edit.name.trim()) return
    try {
      const body = { name: edit.name.trim(), capacity: edit.capacity ? Number(edit.capacity) : null, defaultProctors: edit.defaultProctors }
      const room = edit.id ? await send<Room>('put', `/rooms/${edit.id}`, body) : await send<Room>('post', '/rooms', body)
      rooms.setData((l) => (edit.id ? l?.map((r) => (r.id === room.id ? room : r)) : [...(l ?? []), room].sort((a, b) => a.name.localeCompare(b.name))))
      setEdit(null)
      toast.success(edit.id ? 'Room saved.' : 'Room added.')
    } catch (e) {
      toast.error(errorMessage(e))
    }
  }

  const remove = async (r: Room) => {
    const ok = await confirm({ title: `Delete room ${r.name}?`, body: <p>Quizzes keep the room name they already use.</p>, confirm: 'Delete room', danger: true })
    if (!ok) return
    try {
      await send('delete', `/rooms/${r.id}`)
      rooms.setData((l) => l?.filter((x) => x.id !== r.id))
      toast.success('Room deleted.')
    } catch (e) {
      toast.error(errorMessage(e))
    }
  }

  const row = (r?: Room) => (
    <tr key={r?.id ?? 'new'}>
      <td><input className="input small room" autoFocus value={edit!.name} onChange={(e) => setEdit({ ...edit!, name: e.target.value })} onKeyDown={(e) => e.key === 'Enter' && save()} /></td>
      <td><input className="input small" type="number" min={0} style={{ width: 90 }} value={edit!.capacity} placeholder="—" onChange={(e) => setEdit({ ...edit!, capacity: e.target.value })} /></td>
      <td><input className="input small" type="number" min={1} max={10} style={{ width: 70 }} value={edit!.defaultProctors} onChange={(e) => setEdit({ ...edit!, defaultProctors: Number(e.target.value) || 1 })} /></td>
      <td className="num">{r?.timesUsed ?? 0}</td>
      <td className="nowrap" style={{ textAlign: 'right' }}>
        <button className="icon-btn" aria-label="Save room" onClick={save}><IconCheck /></button>
        <button className="icon-btn" aria-label="Cancel" onClick={() => setEdit(null)}><IconX /></button>
      </td>
    </tr>
  )

  return (
    <section className="sheet">
      <div className="sheet-head">
        <h2>Rooms</h2>
        <button className="btn small" onClick={() => setEdit({ name: '', capacity: '', defaultProctors })} disabled={!!edit && !edit.id}>
          <IconPlus />
          Add room
        </button>
      </div>
      {rooms.error && <ErrorNote message={rooms.error} onRetry={rooms.reload} />}
      {!rooms.data ? (
        <Loading />
      ) : (
        <div className="table-wrap" style={{ maxHeight: 460 }}>
          <table className="grid">
            <thead>
              <tr>
                <th>Room</th>
                <th>Seats</th>
                <th>Proctors by default</th>
                <th className="num">Used in quizzes</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {edit && !edit.id && row()}
              {rooms.data.map((r) =>
                edit?.id === r.id ? (
                  row(r)
                ) : (
                  <tr key={r.id}>
                    <td><span className="room">{r.name}</span></td>
                    <td className={cx(!r.capacity && 'faint')}>{r.capacity ?? '—'}</td>
                    <td>{r.defaultProctors}</td>
                    <td className="num">{r.timesUsed}</td>
                    <td className="nowrap" style={{ textAlign: 'right' }}>
                      <button className="icon-btn" aria-label={`Edit ${r.name}`} onClick={() => setEdit({ id: r.id, name: r.name, capacity: r.capacity?.toString() ?? '', defaultProctors: r.defaultProctors })}><IconEdit /></button>
                      <button className="icon-btn" aria-label={`Delete ${r.name}`} onClick={() => remove(r)}><IconTrash /></button>
                    </td>
                  </tr>
                ),
              )}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
