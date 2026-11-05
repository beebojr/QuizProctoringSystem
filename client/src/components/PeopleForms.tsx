import { useState } from 'react'
import toast from 'react-hot-toast'
import { errorMessage, post, send } from '../api/client'
import type { AffectedAssignment, Day, Excuse, Role, TaSummary, User } from '../api/types'
import { useTerm } from '../contexts/TermContext'
import { TEACHING_DAYS, shortDate, timeRange, todayIso } from '../lib/format'
import { announceChange } from './NotificationBell'
import { Field, Modal, Segmented, Toggle, cx } from './ui'

/** Create or edit a person (TA or admin). */
export function UserModal({ user, defaultDepartment, onClose, onSaved }: { user?: User; defaultDepartment?: string; onClose: () => void; onSaved: (u: User) => void }) {
  const [email, setEmail] = useState(user?.email ?? '')
  const [firstName, setFirstName] = useState(user?.firstName ?? '')
  const [lastName, setLastName] = useState(user?.lastName ?? '')
  const [role, setRole] = useState<Role>(user?.role ?? 'TA')
  const [department, setDepartment] = useState(user?.department ?? defaultDepartment ?? '')
  const [daysOff, setDaysOff] = useState<Day[]>(user?.daysOff ?? [])
  const [password, setPassword] = useState('')
  const [maxOverride, setMaxOverride] = useState(user?.maxSessionsOverride?.toString() ?? '')
  const [targetOverride, setTargetOverride] = useState(user?.targetWorkloadOverride?.toString() ?? '')
  const [canEdit, setCanEdit] = useState(user?.canEditSchedule ?? true)
  const [active, setActive] = useState(user?.isActive ?? true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const toggleDay = (d: Day) => setDaysOff((list) => (list.includes(d) ? list.filter((x) => x !== d) : [...list, d]))

  const save = async () => {
    setError(null)
    if (!email.trim() || !firstName.trim() || !lastName.trim()) return setError('Email, first name and last name are required.')
    if (!user && password.length < 8) return setError('Set a password of at least 8 characters.')
    if (user && password && password.length < 8) return setError('A new password needs at least 8 characters.')
    if (daysOff.length > 3) return setError('Choose at most three days off.')
    setBusy(true)
    try {
      const common = {
        email: email.trim(),
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        role,
        department: department.trim() || null,
        daysOff: role === 'TA' ? daysOff : [],
        maxSessionsOverride: maxOverride === '' ? null : Number(maxOverride),
        targetWorkloadOverride: targetOverride === '' ? null : Number(targetOverride),
        canEditSchedule: canEdit,
      }
      const saved = user
        ? await send<User>('put', `/users/${user.id}`, { ...common, isActive: active, newPassword: password || null })
        : await send<User>('post', '/users', { ...common, password })
      toast.success(user ? 'Details saved.' : `${saved.fullName} added.`)
      announceChange()
      onSaved(saved)
      onClose()
    } catch (e) {
      setError(errorMessage(e))
    } finally {
      setBusy(false)
    }
  }

  return (
    <Modal
      title={user ? `Edit ${user.fullName}` : 'Add a person'}
      subtitle={user ? undefined : 'Teaching assistants proctor quizzes; administrators plan them.'}
      onClose={onClose}
      size="wide"
      footer={
        <>
          <button className="btn" onClick={onClose}>Cancel</button>
          <button className="btn primary" onClick={save} disabled={busy}>
            {busy ? 'Saving…' : user ? 'Save details' : 'Add person'}
          </button>
        </>
      }
    >
      <div className="form-grid">
        <Field label="First name">
          <input className="input" value={firstName} onChange={(e) => setFirstName(e.target.value)} />
        </Field>
        <Field label="Last name">
          <input className="input" value={lastName} onChange={(e) => setLastName(e.target.value)} />
        </Field>
        <Field label="University email">
          <input className="input" type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
        </Field>
        <Field label={user ? 'New password' : 'Password'} hint={user ? 'Leave empty to keep the current password.' : 'At least 8 characters. Share it with them privately.'}>
          <input className="input" type="password" autoComplete="new-password" value={password} onChange={(e) => setPassword(e.target.value)} />
        </Field>
        <Field label="Role">
          <Segmented value={role} onChange={setRole} options={[{ value: 'TA' as Role, label: 'Teaching assistant' }, { value: 'Admin' as Role, label: 'Administrator' }]} label="Role" />
        </Field>
        <Field label="Department">
          <input className="input" value={department} onChange={(e) => setDepartment(e.target.value)} />
        </Field>
        {role === 'TA' && (
          <>
            <Field label="Days off" hint="Never assigned on these days. Friday is off for everyone." className="span-2">
              <div className="row-wrap">
                {TEACHING_DAYS.map((d) => (
                  <button key={d} type="button" className={cx('btn small', daysOff.includes(d) && 'mark')} aria-pressed={daysOff.includes(d)} onClick={() => toggleDay(d)}>
                    {d}
                  </button>
                ))}
              </div>
            </Field>
            <Field label="Semester limit" hint="Most sessions this TA may proctor. Empty uses the default.">
              <input className="input" type="number" min={0} max={200} value={maxOverride} placeholder="Default" onChange={(e) => setMaxOverride(e.target.value)} />
            </Field>
            <Field label="Target workload (points)" hint="The fair share this TA should reach. Empty uses the default.">
              <input className="input" type="number" min={0} max={200} step={0.5} value={targetOverride} placeholder="Default" onChange={(e) => setTargetOverride(e.target.value)} />
            </Field>
            <div className="span-2">
              <Toggle checked={canEdit} onChange={setCanEdit} label="Can edit their teaching schedule" />
            </div>
          </>
        )}
        {user && (
          <div className="span-2">
            <Toggle checked={active} onChange={setActive} label="Active" hint="Inactive people can't sign in and are never assigned." />
          </div>
        )}
      </div>
      {error && <div className="notice short" style={{ marginTop: 14 }} role="alert">{error}</div>}
    </Modal>
  )
}

/** Record an excuse; afterwards lists the sessions that now need a replacement. */
export function ExcuseModal({ userId, taName, isAdmin, onClose, onSaved }: { userId?: string; taName?: string; isAdmin: boolean; onClose: () => void; onSaved: () => void }) {
  const [start, setStart] = useState(todayIso())
  const [end, setEnd] = useState(todayIso())
  const [reason, setReason] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [affected, setAffected] = useState<(AffectedAssignment & { state?: string })[] | null>(null)

  const save = async () => {
    setError(null)
    if (!reason.trim()) return setError('Give a reason.')
    if (end < start) return setError("The end date can't be before the start date.")
    setBusy(true)
    try {
      const r = await send<{ excuse: Excuse; affectedAssignments: AffectedAssignment[] }>('post', '/excuses', { userId: userId ?? null, reason: reason.trim(), startDate: start, endDate: end })
      announceChange()
      onSaved()
      if (r.affectedAssignments.length === 0) {
        toast.success('Excuse saved.')
        onClose()
      } else setAffected(r.affectedAssignments)
    } catch (e) {
      setError(errorMessage(e))
    } finally {
      setBusy(false)
    }
  }

  const replace = async (a: AffectedAssignment) => {
    try {
      const r = await post<{ replaced: boolean; message: string }>(`/assignments/${a.assignmentId}/replace`)
      setAffected((list) => list?.map((x) => (x.assignmentId === a.assignmentId ? { ...x, state: r.message } : x)) ?? null)
      if (r.replaced) toast.success(r.message)
      else toast.error(r.message)
      announceChange()
      onSaved()
    } catch (e) {
      toast.error(errorMessage(e))
    }
  }

  return (
    <Modal
      title={affected ? 'Excuse saved' : taName ? `Excuse ${taName}` : 'Submit an excuse'}
      subtitle={affected ? undefined : 'No proctoring is assigned in this period.'}
      onClose={onClose}
      footer={
        affected ? (
          <>
            {isAdmin && affected.some((a) => !a.state) && (
              <button className="btn" style={{ marginRight: 'auto' }} onClick={async () => { for (const a of affected.filter((x) => !x.state)) await replace(a) }}>
                Replace all
              </button>
            )}
            <button className="btn primary" onClick={onClose}>Done</button>
          </>
        ) : (
          <>
            <button className="btn" onClick={onClose}>Cancel</button>
            <button className="btn primary" onClick={save} disabled={busy}>{busy ? 'Saving…' : 'Save excuse'}</button>
          </>
        )
      }
    >
      {affected ? (
        <div className="stack">
          <div className="notice warn">
            {isAdmin
              ? `${affected.length === 1 ? 'This upcoming session falls' : 'These upcoming sessions fall'} inside the excuse. Replace them now or later from the quiz board.`
              : `${affected.length === 1 ? 'This session falls' : 'These sessions fall'} inside your excuse. The administrators have been told and will find a replacement.`}
          </div>
          {affected.map((a) => (
            <div key={a.assignmentId} className="spread" style={{ padding: '8px 0', borderBottom: '1px solid var(--line)' }}>
              <div>
                <b>{a.courseName}</b>
                <div className="small muted">
                  {shortDate(a.date)}, {timeRange(a.startTime, a.endTime)}, <span className="room">{a.roomName}</span>
                </div>
                {a.state && <div className="small">{a.state}</div>}
              </div>
              {isAdmin && !a.state && (
                <button className="btn small" onClick={() => replace(a)}>Replace</button>
              )}
            </div>
          ))}
        </div>
      ) : (
        <div className="form-grid">
          <Field label="From">
            <input className="input" type="date" value={start} onChange={(e) => { setStart(e.target.value); if (e.target.value > end) setEnd(e.target.value) }} />
          </Field>
          <Field label="To (inclusive)">
            <input className="input" type="date" value={end} min={start} onChange={(e) => setEnd(e.target.value)} />
          </Field>
          <Field label="Reason" className="span-2">
            <textarea className="input" value={reason} maxLength={500} onChange={(e) => setReason(e.target.value)} placeholder="Conference, sick leave, exam of my own…" />
          </Field>
          {error && <div className="notice short span-2" role="alert">{error}</div>}
        </div>
      )}
    </Modal>
  )
}

/** Personal target points and semester limit; empty fields fall back to the defaults. */
export function WorkloadModal({ summary, onClose, onSaved }: { summary: TaSummary; onClose: () => void; onSaved: () => void }) {
  const { settings } = useTerm()
  const [target, setTarget] = useState(summary.targetWorkloadOverride?.toString() ?? '')
  const [max, setMax] = useState(summary.maxSessionsOverride?.toString() ?? '')
  const [busy, setBusy] = useState(false)
  const save = async () => {
    setBusy(true)
    try {
      await send('put', `/users/${summary.userId}/workload`, {
        targetWorkloadOverride: target === '' ? null : Number(target),
        maxSessionsOverride: max === '' ? null : Number(max),
      })
      toast.success('Workload updated.')
      onSaved()
      onClose()
    } catch (e) {
      toast.error(errorMessage(e))
    } finally {
      setBusy(false)
    }
  }
  return (
    <Modal
      title={`Workload of ${summary.name}`}
      subtitle="Leave a field empty to use the department default."
      onClose={onClose}
      footer={
        <>
          <button className="btn" onClick={onClose}>Cancel</button>
          <button className="btn primary" onClick={save} disabled={busy}>Save workload</button>
        </>
      }
    >
      <div className="form-grid">
        <Field label="Target points" hint={`Default ${settings?.defaultTargetWorkload ?? 14}. Fair-share goal used to rank TAs.`}>
          <input className="input" type="number" min={0} max={200} step={0.5} value={target} placeholder={String(settings?.defaultTargetWorkload ?? '')} onChange={(e) => setTarget(e.target.value)} />
        </Field>
        <Field label="Semester limit (sessions)" hint={`Default ${settings?.defaultMaxSessionsPerSemester ?? 20}. Never exceeded automatically.`}>
          <input className="input" type="number" min={0} max={200} value={max} placeholder={String(settings?.defaultMaxSessionsPerSemester ?? '')} onChange={(e) => setMax(e.target.value)} />
        </Field>
      </div>
    </Modal>
  )
}

