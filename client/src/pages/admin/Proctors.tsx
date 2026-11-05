import { useEffect, useMemo, useState } from 'react'
import toast from 'react-hot-toast'
import { Link, useSearchParams } from 'react-router-dom'
import { download, errorMessage, get, send } from '../../api/client'
import type { Excuse, ProctorDetail, TaSummary, User } from '../../api/types'
import { PageHead } from '../../components/AppShell'
import { IconDownload, IconEdit, IconExcuse, IconPlus, IconTrash, IconUserPlus, IconUsers } from '../../components/Icons'
import { announceChange } from '../../components/NotificationBell'
import { ExcuseModal, UserModal, WorkloadModal } from '../../components/PeopleForms'
import { Empty, ErrorNote, Loading, Menu, SearchBox, Segmented, Toggle, cx, useConfirm } from '../../components/ui'
import { useTerm } from '../../contexts/TermContext'
import { useQuery } from '../../lib/hooks'
import { dayMonth, fullDate, initials, plural, points, shortDate, timeRange } from '../../lib/format'

type Filter = 'all' | 'noschedule' | 'excuses' | 'inactive'
type Sort = 'most' | 'least' | 'name'

export default function Proctors() {
  const { semesterId, settings } = useTerm()
  const [params, setParams] = useSearchParams()
  const [search, setSearch] = useState('')
  const [sort, setSort] = useState<Sort>('name')
  const [userModal, setUserModal] = useState<{ user?: User } | null>(null)
  const filter = (params.get('filter') as Filter | null) ?? 'all'
  const tas = useQuery(() => get<TaSummary[]>('/reports/tas', { semesterId }), [semesterId])
  const admins = useQuery(() => get<User[]>('/users', { role: 'Admin' }), [])
  const selectedId = params.get('ta')

  const setParam = (key: string, value: string | null) => {
    const next = new URLSearchParams(params)
    if (value === null) next.delete(key)
    else next.set(key, value)
    setParams(next, { replace: true })
  }

  const list = useMemo(() => {
    const q = search.trim().toLowerCase()
    const rows = (tas.data ?? [])
      .filter((t) => !q || t.name.toLowerCase().includes(q) || t.email.toLowerCase().includes(q) || t.coursesTaught.some((c) => c.toLowerCase().includes(q)))
      .filter((t) =>
        filter === 'noschedule' ? t.slotCount === 0 && t.isActive : filter === 'excuses' ? t.activeExcuses > 0 : filter === 'inactive' ? !t.isActive : true,
      )
    return rows.sort((a, b) => (sort === 'most' ? b.assignments - a.assignments : sort === 'least' ? a.assignments - b.assignments : 0) || a.name.localeCompare(b.name))
  }, [tas.data, search, filter, sort])

  const current = selectedId ?? list[0]?.userId ?? null

  return (
    <>
      <PageHead title="Proctors" sub="Workload, excuses and proctoring history of every teaching assistant.">
        <button className="btn" onClick={() => download('/reports/export/workload', { semesterId }, 'workload.xlsx').catch((e) => toast.error(errorMessage(e)))}>
          <IconDownload />
          Export workload
        </button>
        <button className="btn primary" onClick={() => setUserModal({})}>
          <IconUserPlus />
          Add person
        </button>
      </PageHead>

      {tas.error && <ErrorNote message={tas.error} onRetry={tas.reload} />}
      {tas.loading && !tas.data && <Loading />}
      {tas.data && (
        <div className="split">
          <aside className="sheet list-panel">
            <div className="filters">
              <SearchBox value={search} onChange={setSearch} placeholder="Name, email or course taught" />
              <div className="row small muted">
                <select className="input small grow" aria-label="Show" value={filter} onChange={(e) => setParam('filter', e.target.value === 'all' ? null : e.target.value)}>
                  <option value="all">All TAs ({tas.data.length})</option>
                  <option value="noschedule">Without a schedule</option>
                  <option value="excuses">With an active excuse</option>
                  <option value="inactive">Inactive</option>
                </select>
                <select className="input small grow" aria-label="Sort" value={sort} onChange={(e) => setSort(e.target.value as Sort)}>
                  <option value="name">By name</option>
                  <option value="most">Most sessions</option>
                  <option value="least">Fewest sessions</option>
                </select>
              </div>
            </div>
            <div className="list-scroll">
              {list.map((t) => (
                <button key={t.userId} className={cx('person', t.userId === current && 'on', !t.isActive && 'inactive')} onClick={() => setParam('ta', t.userId)}>
                  <span className="avatar">{initials(t.name)}</span>
                  <span className="grow">
                    <b>{t.name}</b>
                    <span className="sub">{t.email}</span>
                  </span>
                  <span className="meta">
                    <span className={cx('tag', t.targetReached && 'ok')}>{plural(t.assignments, 'session')}</span>
                    {t.activeExcuses > 0 && <span className="tag warn">Excused</span>}
                  </span>
                </button>
              ))}
              {list.length === 0 && <div className="empty">Nobody matches.</div>}
              <div className="list-section">Administrators</div>
              {(admins.data ?? []).map((a) => (
                <button key={a.id} className="person" onClick={() => setUserModal({ user: a })}>
                  <span className="avatar">{initials(a.fullName)}</span>
                  <span className="grow">
                    <b>{a.fullName}</b>
                    <span className="sub">{a.email}</span>
                  </span>
                </button>
              ))}
            </div>
          </aside>
          {current ? (
            <Detail key={current} userId={current} semesterId={semesterId} onListChanged={tas.reload} onEdit={(u) => setUserModal({ user: u })} onDeleted={() => { setParam('ta', null); void tas.reload() }} />
          ) : (
            <div className="sheet">
              <Empty icon={<IconUsers />} title="No teaching assistants">
                Add the first TA with “Add person”.
              </Empty>
            </div>
          )}
        </div>
      )}
      {userModal && (
        <UserModal
          user={userModal.user}
          defaultDepartment={settings?.departmentName}
          onClose={() => setUserModal(null)}
          onSaved={(u) => {
            void tas.reload()
            void admins.reload()
            if (u.role === 'TA') setParam('ta', u.id)
            window.dispatchEvent(new Event('qps:person-saved'))
          }}
        />
      )}
    </>
  )
}

function Detail({ userId, semesterId, onListChanged, onEdit, onDeleted }: { userId: string; semesterId?: string; onListChanged: () => void; onEdit: (u: User) => void; onDeleted: () => void }) {
  const confirm = useConfirm()
  const detail = useQuery(() => get<ProctorDetail>(`/reports/tas/${userId}`, { semesterId }), [userId, semesterId])
  const user = useQuery(() => get<User>(`/users/${userId}`), [userId])
  const [excusing, setExcusing] = useState(false)
  const [workload, setWorkload] = useState(false)
  const [historyFilter, setHistoryFilter] = useState<'all' | 'upcoming' | 'past'>('all')

  const { settings } = useTerm()

  // Reload when the edit modal saves this person.
  useEffect(() => {
    const onSaved = () => {
      void detail.reload()
      void user.reload()
    }
    window.addEventListener('qps:person-saved', onSaved)
    return () => window.removeEventListener('qps:person-saved', onSaved)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [userId])

  if (detail.error) return <ErrorNote message={detail.error} onRetry={detail.reload} />
  if (!detail.data || !user.data) return <Loading />
  const s = detail.data.summary
  const u = user.data
  const ratio = s.targetWorkload > 0 ? s.points / s.targetWorkload : 0
  const history = detail.data.history.filter((h) => historyFilter === 'all' || (historyFilter === 'upcoming' ? h.status === 'Upcoming' : h.status !== 'Upcoming'))

  const reloadBoth = async () => {
    await detail.reload()
    onListChanged()
  }

  const setEditing = async (enabled: boolean) => {
    try {
      await send('put', `/users/${userId}/schedule-editing`, { enabled })
      detail.setData((d) => d && { ...d, summary: { ...d.summary, canEditSchedule: enabled } })
      toast.success(enabled ? 'Schedule editing allowed.' : 'Schedule locked.')
      onListChanged()
    } catch (e) {
      toast.error(errorMessage(e))
    }
  }

  const removeExcuse = async (x: Excuse) => {
    const ok = await confirm({ title: 'Delete this excuse?', body: <p>{s.name} can be assigned again from {fullDate(x.startDate)} to {fullDate(x.endDate)}.</p>, confirm: 'Delete excuse', danger: true })
    if (!ok) return
    try {
      await send('delete', `/excuses/${x.id}`)
      toast.success('Excuse deleted.')
      announceChange()
      await reloadBoth()
    } catch (e) {
      toast.error(errorMessage(e))
    }
  }

  const setActive = async (active: boolean) => {
    try {
      await send('put', `/users/${userId}`, {
        email: u.email, firstName: u.firstName, lastName: u.lastName, role: u.role, department: u.department, daysOff: u.daysOff,
        maxSessionsOverride: u.maxSessionsOverride ?? null, targetWorkloadOverride: u.targetWorkloadOverride ?? null, canEditSchedule: u.canEditSchedule, isActive: active,
      })
      toast.success(active ? `${s.name} is active again.` : `${s.name} is deactivated and won't be assigned.`)
      await Promise.all([user.reload(), reloadBoth()])
    } catch (e) {
      toast.error(errorMessage(e))
    }
  }

  const remove = async () => {
    const ok = await confirm({
      title: `Delete ${s.name}?`,
      body: <p>Upcoming sessions ({s.upcoming}) are freed for someone else. Past proctoring stays in the history and reports. To keep the account but stop assignments, deactivate instead.</p>,
      confirm: 'Delete person',
      danger: true,
    })
    if (!ok) return
    try {
      await send('delete', `/users/${userId}`)
      toast.success(`${s.name} deleted.`)
      announceChange()
      onDeleted()
    } catch (e) {
      toast.error(errorMessage(e))
    }
  }

  return (
    <section className="stack">
      <div className="sheet sheet-pad stack">
        <div className="spread" style={{ alignItems: 'flex-start' }}>
          <div className="row" style={{ alignItems: 'flex-start' }}>
            <span className="avatar lg">{initials(s.name)}</span>
            <div className="stack-s">
              <h2>
                {s.name} {!s.isActive && <span className="tag short">Inactive</span>}
              </h2>
              <span className="muted small">
                {s.email}. {s.daysOff.length ? `Day off: ${s.daysOff.join(', ')}.` : 'No day off.'}
              </span>
              {s.coursesTaught.length > 0 && (
                <div className="row-wrap">
                  {s.coursesTaught.map((c) => (
                    <span key={c} className="tag ta">{c}</span>
                  ))}
                </div>
              )}
            </div>
          </div>
          <div className="row">
            <button className="btn small" onClick={() => onEdit(u)}>
              <IconEdit />
              Edit
            </button>
            <Menu
              items={[
                { label: s.isActive ? 'Deactivate' : 'Activate', onClick: () => setActive(!s.isActive) },
                { label: 'Delete person', icon: <IconTrash />, onClick: remove, danger: true, divider: true },
              ]}
            />
          </div>
        </div>
        <div className="kv">
          <div><b>{s.assignments}</b><span>Sessions this semester</span></div>
          <div><b>{s.upcoming}</b><span>Upcoming</span></div>
          <div><b>{s.completed}</b><span>Completed</span></div>
          <div><b>{s.thisWeek}<small className="faint" style={{ fontSize: 14 }}> / {settings?.maxSessionsPerWeek ?? 2}</small></b><span>This week</span></div>
        </div>
      </div>

      <div className="overview" style={{ gridTemplateColumns: 'minmax(0,1fr) minmax(0,1fr)' }}>
        <div className="sheet sheet-pad stack">
          <div className="spread">
            <h3>Workload</h3>
            <button className="btn ghost small" onClick={() => setWorkload(true)}>
              <IconEdit />
              Adjust
            </button>
          </div>
          <div className="load">
            <div className="spread small">
              <span><b style={{ fontSize: 18, fontFamily: 'var(--f-display)' }}>{points(s.points)}</b> of {points(s.targetWorkload)} target points</span>
              <span className="muted">limit {s.maxSessions} sessions</span>
            </div>
            <div className="load-bar">
              <i className={cx(ratio >= 1 && 'done', s.assignments > s.maxSessions && 'over')} style={{ width: `${Math.min(100, ratio * 100)}%` }} />
            </div>
            <span className="small muted">
              {s.targetWorkloadOverride != null || s.maxSessionsOverride != null ? 'Personal targets set. ' : 'Using the department defaults. '}
              {ratio >= 1 ? 'Target reached; new seats go to others first.' : `${points(Math.max(0, s.targetWorkload - s.points))} points to go.`}
            </span>
          </div>
          <Toggle checked={s.canEditSchedule} onChange={setEditing} label="Can edit teaching schedule" hint={<><Link to={`/schedules?ta=${userId}`}>Open timetable</Link> ({plural(s.slotCount, 'class', 'classes')})</>} />
        </div>

        <div className="sheet sheet-pad stack">
          <div className="spread">
            <h3>Excuses</h3>
            <button className="btn ghost small" onClick={() => setExcusing(true)} disabled={!s.isActive}>
              <IconPlus />
              Add excuse
            </button>
          </div>
          {detail.data.excuses.length === 0 ? (
            <p className="muted small">No excuses this semester.</p>
          ) : (
            <div className="stack-s">
              {detail.data.excuses.map((x) => (
                <div key={x.id} className="spread" style={{ padding: '6px 0', borderBottom: '1px solid var(--line)' }}>
                  <div className="small">
                    <b>{dayMonth(x.startDate)}{x.endDate !== x.startDate ? ` – ${dayMonth(x.endDate)}` : ''}</b>{' '}
                    {x.isActive ? <span className="tag warn">Active</span> : <span className="faint">Past</span>}
                    <div className="muted">{x.reason}{x.submittedByTa ? ' (submitted by the TA)' : ''}</div>
                  </div>
                  <button className="icon-btn" aria-label="Delete excuse" onClick={() => removeExcuse(x)}>
                    <IconTrash />
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      <div className="sheet">
        <div className="sheet-head">
          <h3>Proctoring history</h3>
          <Segmented value={historyFilter} onChange={setHistoryFilter} options={[{ value: 'all', label: 'All' }, { value: 'upcoming', label: 'Upcoming' }, { value: 'past', label: 'Past' }]} label="History filter" />
        </div>
        <div className="sheet-pad" style={{ paddingTop: 6, paddingBottom: 6 }}>
          {history.length === 0 ? (
            <Empty icon={<IconExcuse />} title="No sessions here" />
          ) : (
            <div className="history">
              {history.map((h) => (
                <div key={h.assignmentId}>
                  <span className="when">
                    <b>{shortDate(h.date)}</b>
                    {timeRange(h.startTime, h.endTime)}
                  </span>
                  <span className="what">
                    <b>
                      <Link to={`/quizzes?focus=${h.quizId}&status=All`} style={{ color: 'inherit' }}>{h.courseName}</Link>
                    </b>
                    <span>
                      {h.isReserve ? 'Reserve' : <span className="room">{h.roomName}</span>}
                      {h.group ? `, ${h.group}` : ''}
                      {h.coProctors.length ? `. With ${h.coProctors.join(', ')}` : ''}
                    </span>
                  </span>
                  <span className="row">
                    {h.isCourseTa && <span className="tag ta">Course TA</span>}
                    {h.source === 'Manual' && <span className="tag line">By hand</span>}
                    <span className={cx('status', h.status)}>{h.status}</span>
                  </span>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {excusing && <ExcuseModal userId={userId} taName={s.name} isAdmin onClose={() => setExcusing(false)} onSaved={reloadBoth} />}
      {workload && <WorkloadModal summary={s} onClose={() => setWorkload(false)} onSaved={reloadBoth} />}
    </section>
  )
}
