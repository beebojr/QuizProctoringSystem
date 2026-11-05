import { useMemo, useState } from 'react'
import toast from 'react-hot-toast'
import { Link, useSearchParams } from 'react-router-dom'
import { api, download, errorMessage, get, send } from '../../api/client'
import type { Day, Result, ScheduleSlot, TaScheduleSummary } from '../../api/types'
import { PageHead } from '../../components/AppShell'
import { IconClock, IconDownload, IconLock, IconUpload } from '../../components/Icons'
import { announceChange } from '../../components/NotificationBell'
import { ScheduleGrid, ScheduleLegend, SlotEditor } from '../../components/ScheduleGrid'
import { Empty, ErrorNote, Loading, Menu, Modal, SearchBox, Toggle, cx, useConfirm } from '../../components/ui'
import { useTerm } from '../../contexts/TermContext'
import { useQuery } from '../../lib/hooks'
import { SHORT_DAY, initials, plural } from '../../lib/format'

export default function Schedules() {
  const { semesterId } = useTerm()
  const [params, setParams] = useSearchParams()
  const confirm = useConfirm()
  const [search, setSearch] = useState('')
  const [editor, setEditor] = useState<{ slot?: ScheduleSlot; day?: Day; slotNumber?: number } | null>(null)
  const [importing, setImporting] = useState(false)
  const overview = useQuery(() => get<TaScheduleSummary[]>('/schedules/overview', { semesterId }), [semesterId])
  const selectedId = params.get('ta') ?? overview.data?.[0]?.userId ?? null
  const selected = overview.data?.find((t) => t.userId === selectedId) ?? null
  const slots = useQuery(() => get<ScheduleSlot[]>('/schedules', { userId: selectedId, semesterId }), [selectedId, semesterId], !!selectedId)

  const list = useMemo(() => {
    const q = search.trim().toLowerCase()
    return (overview.data ?? []).filter((t) => !q || t.name.toLowerCase().includes(q) || t.email.toLowerCase().includes(q))
  }, [overview.data, search])
  const noSchedule = (overview.data ?? []).filter((t) => t.slotCount === 0 && t.isActive).length
  const unlocked = (overview.data ?? []).filter((t) => t.canEditSchedule).length

  const reloadAll = async () => {
    await Promise.all([overview.reload(), slots.reload()])
  }

  const setEditing = async (enabled: boolean) => {
    if (!selected) return
    try {
      await send('put', `/users/${selected.userId}/schedule-editing`, { enabled })
      overview.setData((l) => l?.map((t) => (t.userId === selected.userId ? { ...t, canEditSchedule: enabled } : t)))
      toast.success(enabled ? `${selected.name} can edit their schedule.` : `${selected.name}'s schedule is locked.`)
    } catch (e) {
      toast.error(errorMessage(e))
    }
  }

  const setEditingForAll = async (enabled: boolean) => {
    const ok = await confirm({
      title: enabled ? 'Let every TA edit their schedule?' : 'Lock every TA schedule?',
      body: <p>{enabled ? 'TAs can add, change and remove their own classes until you lock schedules again.' : 'TAs can still see their schedule but only admins can change it. Lock schedules once the semester timetable is final.'}</p>,
      confirm: enabled ? 'Allow editing' : 'Lock schedules',
    })
    if (!ok) return
    try {
      await send('put', '/users/schedule-editing', { enabled })
      overview.setData((l) => l?.map((t) => ({ ...t, canEditSchedule: enabled })))
      toast.success(enabled ? 'All TAs can edit their schedules.' : 'All schedules are locked.')
    } catch (e) {
      toast.error(errorMessage(e))
    }
  }

  return (
    <>
      <PageHead
        title="TA schedules"
        sub={`Teaching timetables decide who is free for each quiz. ${noSchedule > 0 ? `${plural(noSchedule, 'TA')} ${noSchedule === 1 ? 'has' : 'have'} no classes entered yet.` : 'Every active TA has a timetable.'}`}
      >
        <Menu
          label="Schedule editing for everyone"
          trigger={(toggle) => (
            <button className="btn" onClick={toggle}>
              <IconLock />
              Editing: {unlocked} of {overview.data?.length ?? 0} open
            </button>
          )}
          items={[
            { label: 'Allow every TA to edit', onClick: () => setEditingForAll(true) },
            { label: 'Lock every schedule', onClick: () => setEditingForAll(false) },
          ]}
        />
        <button className="btn" onClick={() => setImporting(true)}>
          <IconUpload />
          Import from Excel
        </button>
      </PageHead>

      {overview.error && <ErrorNote message={overview.error} onRetry={overview.reload} />}
      {overview.loading && !overview.data && <Loading />}
      {overview.data && (
        <div className="split">
          <aside className="sheet list-panel">
            <div className="filters">
              <SearchBox value={search} onChange={setSearch} placeholder="Search TAs" />
            </div>
            <div className="list-scroll">
              {list.map((t) => (
                <button key={t.userId} className={cx('person', t.userId === selectedId && 'on', !t.isActive && 'inactive')} onClick={() => setParams({ ta: t.userId }, { replace: true })}>
                  <span className="avatar">{initials(t.name)}</span>
                  <span className="grow">
                    <b>{t.name}</b>
                    <span className="sub">{t.daysOff.length ? `Off ${t.daysOff.map((d) => SHORT_DAY[d as Day] ?? d).join(', ')}` : 'No day off'}</span>
                  </span>
                  <span className="meta">
                    <span className={cx('tag', t.slotCount === 0 ? 'short' : '')}>{t.slotCount === 0 ? 'Empty' : plural(t.slotCount, 'class', 'classes')}</span>
                    {!t.canEditSchedule && <IconLock width={13} height={13} aria-label="Locked" />}
                  </span>
                </button>
              ))}
              {list.length === 0 && <div className="empty">No TA matches.</div>}
            </div>
          </aside>

          <section className="stack">
            {!selected ? (
              <div className="sheet">
                <Empty icon={<IconClock />} title="No teaching assistants yet">
                  Add TAs on the <Link to="/proctors">Proctors</Link> page.
                </Empty>
              </div>
            ) : (
              <>
                <div className="sheet sheet-pad spread" style={{ flexWrap: 'wrap' }}>
                  <div className="row">
                    <span className="avatar lg">{initials(selected.name)}</span>
                    <div>
                      <h2>{selected.name}</h2>
                      <span className="muted small">
                        {selected.email}. {selected.daysOff.length ? `Day off: ${selected.daysOff.join(', ')}.` : 'No day off set.'}{' '}
                        <Link to={`/proctors?ta=${selected.userId}`}>Profile</Link>
                      </span>
                    </div>
                  </div>
                  <Toggle checked={selected.canEditSchedule} onChange={setEditing} label="TA can edit" hint={selected.canEditSchedule ? 'Changes by the TA notify admins.' : 'Only admins can change it.'} />
                </div>
                <div className="spread">
                  <ScheduleLegend />
                  <span className="muted small">{plural(slots.data?.length ?? 0, 'class', 'classes')} a week. Click an empty cell to add one.</span>
                </div>
                {slots.error && <ErrorNote message={slots.error} onRetry={slots.reload} />}
                {slots.data ? (
                  <ScheduleGrid
                    slots={slots.data}
                    daysOff={selected.daysOff as Day[]}
                    editable
                    onAdd={(day, slotNumber) => setEditor({ day, slotNumber })}
                    onEdit={(slot) => setEditor({ slot })}
                  />
                ) : (
                  <Loading />
                )}
              </>
            )}
          </section>
        </div>
      )}

      {editor && selected && (
        <SlotEditor {...editor} userId={selected.userId} semesterId={semesterId} onClose={() => setEditor(null)} onSaved={reloadAll} />
      )}
      {importing && <ImportSchedules semesterId={semesterId} onClose={() => setImporting(false)} onDone={reloadAll} />}
    </>
  )
}

interface ImportResult {
  created: number
  tasReplaced: number
  errors: { row: number; message: string }[]
}

function ImportSchedules({ semesterId, onClose, onDone }: { semesterId?: string; onClose: () => void; onDone: () => void }) {
  const [file, setFile] = useState<File | null>(null)
  const [replace, setReplace] = useState(true)
  const [busy, setBusy] = useState(false)
  const [result, setResult] = useState<ImportResult | null>(null)
  const [error, setError] = useState<string | null>(null)

  const run = async () => {
    if (!file) return
    setBusy(true)
    setError(null)
    try {
      const form = new FormData()
      form.append('file', file)
      if (semesterId) form.append('semesterId', semesterId)
      form.append('replaceExisting', String(replace))
      const { data } = await api.post<Result<ImportResult>>('/schedules/import', form)
      if (!data.isSuccess) throw new Error(data.message ?? 'Import failed.')
      setResult(data.value!)
      announceChange()
      onDone()
    } catch (e) {
      setError(errorMessage(e))
    } finally {
      setBusy(false)
    }
  }

  return (
    <Modal
      title="Import teaching schedules"
      subtitle="One row per class: Email, Day, Slot, Course, Type, Room."
      onClose={onClose}
      footer={
        result ? (
          <button className="btn primary" onClick={onClose}>Done</button>
        ) : (
          <>
            <button className="btn" onClick={() => download('/schedules/import/template', undefined, 'TA-schedule-template.xlsx').catch((e) => toast.error(errorMessage(e)))}>
              <IconDownload />
              Template
            </button>
            <button className="btn primary" disabled={!file || busy} onClick={run}>
              {busy ? 'Importing…' : 'Import'}
            </button>
          </>
        )
      }
    >
      {result ? (
        <div className="stack">
          <div className={cx('notice', result.errors.length ? 'warn' : 'ok')}>
            Added {plural(result.created, 'class', 'classes')}
            {result.tasReplaced > 0 ? `, replacing the timetables of ${plural(result.tasReplaced, 'TA')}` : ''}.
            {result.errors.length > 0 && ` ${plural(result.errors.length, 'row')} could not be read.`}
          </div>
          {result.errors.length > 0 && (
            <div className="table-wrap" style={{ maxHeight: 260 }}>
              <table className="grid">
                <tbody>
                  {result.errors.map((e, i) => (
                    <tr key={i}>
                      <td className="nowrap muted">Row {e.row}</td>
                      <td>{e.message}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      ) : (
        <div className="stack">
          <label className="drop">
            <IconUpload />
            <b>{file ? file.name : 'Choose an Excel file (.xlsx)'}</b>
            <span className="muted small">Day accepts names like Sunday or Sun; slot accepts 1–5 or 1st–5th.</span>
            <input type="file" accept=".xlsx,.xlsm" className="sr-only" onChange={(e) => setFile(e.target.files?.[0] ?? null)} />
          </label>
          <Toggle checked={replace} onChange={setReplace} label="Replace existing timetables" hint="TAs in the file lose their current classes first. TAs not in the file are untouched." />
          {error && <div className="notice short">{error}</div>}
        </div>
      )}
    </Modal>
  )
}
