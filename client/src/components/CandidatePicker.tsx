import { useMemo, useState } from 'react'
import toast from 'react-hot-toast'
import { errorMessage, get, post } from '../api/client'
import type { AssignOutcome, Candidate, Candidates } from '../api/types'
import { useQuery } from '../lib/hooks'
import { longDate, plural, points, timeRange } from '../lib/format'
import { IconAlert, IconFlag, IconSeat } from './Icons'
import { SlotRail, SlotRailScale } from './SlotRail'
import { ErrorNote, Loading, Modal, SearchBox, Toggle, cx } from './ui'
import { announceChange } from './NotificationBell'

/** Choose a TA for one room of a quiz, with every rule explained per person. */
export function CandidatePicker({ locationId, onClose, onAssigned }: { locationId: string; onClose: () => void; onAssigned: () => void }) {
  const q = useQuery(() => get<Candidates>(`/assignments/candidates/${locationId}`), [locationId])
  const [search, setSearch] = useState('')
  const [showAll, setShowAll] = useState(false)
  const [forcing, setForcing] = useState<string | null>(null)
  const [reason, setReason] = useState('')
  const [busy, setBusy] = useState<string | null>(null)

  const data = q.data
  const list = useMemo(() => {
    if (!data) return []
    const s = search.trim().toLowerCase()
    return data.candidates.filter((c) => (showAll || c.eligible || s) && (!s || c.name.toLowerCase().includes(s) || c.email.toLowerCase().includes(s)))
  }, [data, search, showAll])
  const eligibleCount = data?.candidates.filter((c) => c.eligible).length ?? 0
  const hiddenCount = (data?.candidates.length ?? 0) - eligibleCount

  const assign = async (c: Candidate, force: boolean) => {
    if (force && reason.trim().length < 3) {
      toast.error('Write a short reason for the override.')
      return
    }
    setBusy(c.userId)
    try {
      const outcome = await post<AssignOutcome>('/assignments', {
        locationId,
        userId: c.userId,
        force,
        overrideReason: force ? reason.trim() : null,
      })
      if (!outcome.assigned) {
        toast.error(outcome.message || outcome.violations.map((v) => v.message).join(' '))
        await q.reload()
        return
      }
      toast.success(outcome.message)
      announceChange()
      onAssigned()
      setForcing(null)
      setReason('')
      const filled = (data?.assigned ?? 0) + 1
      if (data && filled >= data.proctorsNeeded) onClose()
      else await q.reload()
    } catch (e) {
      toast.error(errorMessage(e))
    } finally {
      setBusy(null)
    }
  }

  const quizWindow = data ? { start: data.startTime, end: data.endTime } : undefined

  return (
    <Modal
      size="xwide"
      icon={<IconSeat width={22} height={22} />}
      title={data ? `Proctor for ${data.isReserve ? 'the reserve' : data.roomName}` : 'Choose a proctor'}
      subtitle={
        data && (
          <>
            {data.courseName}, {longDate(data.date)}, {timeRange(data.startTime, data.endTime)}. {data.assigned} of {data.proctorsNeeded} seats filled.
          </>
        )
      }
      onClose={onClose}
    >
      {q.error && <ErrorNote message={q.error} onRetry={q.reload} />}
      {!data && !q.error && <Loading />}
      {data && (
        <div className="stack">
          <div className="spread" style={{ flexWrap: 'wrap' }}>
            <SearchBox value={search} onChange={setSearch} placeholder="Search all TAs" className="grow" />
            <Toggle checked={showAll} onChange={setShowAll} label={`Show unavailable TAs (${hiddenCount})`} />
          </div>
          <div className="legend">
            <span><i style={{ background: 'var(--hl)' }} />Quiz time</span>
            <span><i style={{ background: 'var(--ink-2)' }} />Teaching</span>
            <span><i style={{ background: 'var(--short)' }} />Teaching clash</span>
            <span><IconFlag width={12} height={12} style={{ color: 'var(--ta)', verticalAlign: -1, marginRight: 6 }} />Teaches this course</span>
          </div>
          <p className="small muted">
            {eligibleCount === 0
              ? 'Nobody passes every rule for this seat. Turn on “Show unavailable TAs” to override a rule with a reason.'
              : `${plural(eligibleCount, 'TA')} can take this seat without breaking a rule, best match first.`}
          </p>
          <div className="cand-list">
            <div className="cand" style={{ paddingTop: 0, paddingBottom: 4 }} aria-hidden="true">
              <span />
              <SlotRailScale />
              <span />
              <span />
            </div>
            {list.length === 0 && search.trim() && <div className="empty">No TA matches “{search.trim()}”.</div>}
            {list.map((c) => {
              const ratio = c.targetWorkload > 0 ? c.points / c.targetWorkload : 0
              return (
                <div key={c.userId} className={cx('cand', !c.eligible && 'ineligible')}>
                  <div className="who">
                    <b>
                      {c.name}
                      {c.isCourseTa && <IconFlag aria-label="Teaches this course" />}
                    </b>
                    <span>{c.email}</span>
                  </div>
                  <div className="rails">
                    <SlotRail
                      window={quizWindow}
                      teaching={c.teachingThatDay.map((t) => ({ start: t.startTime, end: t.endTime, label: t.courseName, title: `${t.courseName} (${t.type}), ${timeRange(t.startTime, t.endTime)}` }))}
                    />
                  </div>
                  <div className="loads">
                    <span className={cx(c.sessionsThisWeek >= c.maxPerWeek && 'over')}>
                      This week <b>{c.sessionsThisWeek}</b> of {c.maxPerWeek}
                    </span>
                    <span className={cx(c.sessionsThisSemester >= c.maxSessions && 'over')}>
                      Semester <b>{c.sessionsThisSemester}</b> of {c.maxSessions}
                    </span>
                    <span>
                      Points <b>{points(c.points)}</b> / {points(c.targetWorkload)}
                      {ratio >= 1 && ' (target met)'}
                    </span>
                  </div>
                  <div>
                    {c.eligible ? (
                      <button className="btn primary small" disabled={busy !== null} onClick={() => assign(c, false)}>
                        Assign
                      </button>
                    ) : (
                      <button className="btn small" disabled={busy !== null} onClick={() => { setForcing(forcing === c.userId ? null : c.userId); setReason('') }}>
                        Override
                      </button>
                    )}
                  </div>
                  {!c.eligible && (
                    <div className="why">
                      <IconAlert width={13} height={13} style={{ verticalAlign: -2, marginRight: 5 }} />
                      {c.violations.map((v) => v.message).join(' ')}
                    </div>
                  )}
                  {forcing === c.userId && (
                    <div className="force-box">
                      <input
                        className="input small grow"
                        autoFocus
                        placeholder="Why is this rule being broken? (kept with the assignment)"
                        value={reason}
                        onChange={(e) => setReason(e.target.value)}
                        onKeyDown={(e) => e.key === 'Enter' && assign(c, true)}
                      />
                      <button className="btn danger solid small" disabled={busy !== null} onClick={() => assign(c, true)}>
                        Assign anyway
                      </button>
                    </div>
                  )}
                </div>
              )
            })}
          </div>
        </div>
      )}
    </Modal>
  )
}
