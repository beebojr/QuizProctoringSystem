import { useState } from 'react'
import toast from 'react-hot-toast'
import { useNavigate } from 'react-router-dom'
import { errorMessage, get, post, send } from '../api/client'
import type { AutoAssignSummary, ProctorChip, Quiz, QuizLocation } from '../api/types'
import { slotLabel, timeRange, time12, hhmm, plural } from '../lib/format'
import { CandidatePicker } from './CandidatePicker'
import { IconAlert, IconBolt, IconCopy, IconEdit, IconFlag, IconPlus, IconSwap, IconTrash, IconUserMinus, IconUsers, IconX } from './Icons'
import { announceChange } from './NotificationBell'
import { Menu, cx, useConfirm } from './ui'

interface Props {
  quiz: Quiz
  onReplace: (q: Quiz) => void
  onRemove: (id: string) => void
}

export function QuizCard({ quiz, onReplace, onRemove }: Props) {
  const navigate = useNavigate()
  const confirm = useConfirm()
  const [picking, setPicking] = useState<string | null>(null)
  const upcoming = quiz.status === 'Upcoming'
  const short = quiz.proctorsAssigned < quiz.proctorsNeeded

  const refresh = async () => {
    try {
      onReplace(await get<Quiz>(`/quizzes/${quiz.id}`))
    } catch (e) {
      toast.error(errorMessage(e))
    }
  }

  const fill = async () => {
    try {
      const summary = await send<AutoAssignSummary>('post', `/assignments/auto-assign/quiz/${quiz.id}`)
      if (summary.assigned === 0 && summary.missing === 0) toast('Every seat is already filled.')
      else if (summary.missing === 0) toast.success(`Filled ${plural(summary.assigned, 'seat')}.`)
      else toast(`Filled ${summary.assigned}; ${plural(summary.missing, 'seat')} still open. ${summary.shortfalls[0]?.reasons.join(', ') ?? ''}`, { icon: '⚠️', duration: 7000 })
      announceChange()
      await refresh()
    } catch (e) {
      toast.error(errorMessage(e))
    }
  }

  const clear = async () => {
    const ok = await confirm({
      title: 'Remove every proctor from this quiz?',
      body: <p>All {quiz.proctorsAssigned} proctors of {quiz.courseName} are removed and notified. The rooms stay.</p>,
      confirm: 'Remove proctors',
      danger: true,
    })
    if (!ok) return
    try {
      await send('delete', `/assignments/quiz/${quiz.id}`)
      toast.success('Proctors removed.')
      announceChange()
      await refresh()
    } catch (e) {
      toast.error(errorMessage(e))
    }
  }

  const remove = async () => {
    const ok = await confirm({
      title: `Delete ${quiz.courseName} on this date?`,
      body: <p>The quiz, its rooms and {plural(quiz.proctorsAssigned, 'assignment')} are deleted. Assigned TAs of an upcoming quiz are notified.</p>,
      confirm: 'Delete quiz',
      danger: true,
    })
    if (!ok) return
    try {
      await send('delete', `/quizzes/${quiz.id}`)
      toast.success('Quiz deleted.')
      announceChange()
      onRemove(quiz.id)
    } catch (e) {
      toast.error(errorMessage(e))
    }
  }

  return (
    <article className={cx('quiz', short && 'short-staffed', quiz.status === 'Started' && 'is-started', quiz.status === 'Finished' && 'is-finished')}>
      <div className="quiz-time">
        <span className="slot">{quiz.slotNumber === null || quiz.slotNumber === undefined ? hhmm(quiz.startTime) : slotLabel(quiz.slotNumber)}</span>
        <span className="t">{quiz.slotNumber === null || quiz.slotNumber === undefined ? `to ${time12(quiz.endTime)}` : timeRange(quiz.startTime, quiz.endTime)}</span>
        <span className="wk">Week {quiz.weekNumber}</span>
      </div>
      <div className="quiz-body">
        <div className="quiz-top">
          <h3>{quiz.courseName}</h3>
          {quiz.examType !== 'Quiz' && <span className="tag ink">{quiz.examType}</span>}
          <span className={cx('status', quiz.status)}>{quiz.status}</span>
          <Menu
            items={[
              { label: 'Edit quiz', icon: <IconEdit />, onClick: () => navigate(`/quizzes/${quiz.id}/edit`) },
              { label: 'Duplicate', icon: <IconCopy />, onClick: () => navigate(`/quizzes/new?from=${quiz.id}`) },
              { label: 'Fill empty seats', icon: <IconBolt />, onClick: fill, hidden: !upcoming || !short },
              { label: 'Remove all proctors', icon: <IconUserMinus />, onClick: clear, hidden: !upcoming || quiz.proctorsAssigned === 0 },
              { label: 'Delete quiz', icon: <IconTrash />, onClick: remove, danger: true, divider: true },
            ]}
          />
        </div>
        <div className="quiz-sub">
          <span className={cx('staff', short ? 'short' : 'ok')}>
            {quiz.proctorsAssigned} of {quiz.proctorsNeeded} seats filled
          </span>
          <span>{plural(quiz.locations.filter((l) => !l.isReserve).length, 'room')}</span>
          {quiz.warningCount > 0 && (
            <span className="tag warn">
              <IconAlert />
              {plural(quiz.warningCount, 'rule override')}
            </span>
          )}
          {quiz.source === 'Import' && <span className="faint">Imported</span>}
        </div>
        {quiz.locations.map((loc) => (
          <LocationRow key={loc.id} loc={loc} upcoming={upcoming} onPick={() => setPicking(loc.id)} onChanged={refresh} />
        ))}
        {quiz.notes && <p className="quiz-notes">{quiz.notes}</p>}
      </div>
      {picking && <CandidatePicker locationId={picking} onClose={() => setPicking(null)} onAssigned={refresh} />}
    </article>
  )
}

function LocationRow({ loc, upcoming, onPick, onChanged }: { loc: QuizLocation; upcoming: boolean; onPick: () => void; onChanged: () => void }) {
  const open = Math.max(0, loc.proctorsNeeded - loc.proctors.length)
  const full = loc.assigned >= loc.proctorsNeeded
  return (
    <div className={cx('loc', loc.isReserve && 'reserve')}>
      <div className="loc-head">
        <span className="room">{loc.isReserve ? 'Reserve' : loc.roomName}</span>
        {loc.group && <span className="tag line">{loc.group}</span>}
        <span className="need">
          <IconUsers width={13} height={13} style={{ verticalAlign: -2, marginRight: 3 }} />
          needs {loc.proctorsNeeded}
        </span>
        <span className={cx('count', full ? 'ok' : 'short')}>
          {loc.assigned}/{loc.proctorsNeeded}
        </span>
      </div>
      <div className="seats">
        {loc.proctors.map((p) => (
          <SeatChip key={p.assignmentId} chip={p} upcoming={upcoming} onChanged={onChanged} />
        ))}
        {Array.from({ length: open }, (_, i) => (
          <button key={i} className={cx('seat open', loc.isReserve && 'reserve')} onClick={onPick} aria-label={`Add a proctor to ${loc.isReserve ? 'the reserve' : loc.roomName}`}>
            <IconPlus />
            {i === 0 ? 'Add proctor' : 'Open seat'}
          </button>
        ))}
      </div>
    </div>
  )
}

function SeatChip({ chip, upcoming, onChanged }: { chip: ProctorChip; upcoming: boolean; onChanged: () => void }) {
  const navigate = useNavigate()
  const confirm = useConfirm()
  const flagged = chip.warnings.length > 0

  const replace = async () => {
    try {
      const r = await post<{ replaced: boolean; newProctorName?: string | null; message: string }>(`/assignments/${chip.assignmentId}/replace`)
      if (r.replaced) toast.success(r.message)
      else toast.error(r.message)
      announceChange()
      onChanged()
    } catch (e) {
      toast.error(errorMessage(e))
    }
  }

  const remove = async () => {
    const ok = await confirm({
      title: `Remove ${chip.name}?`,
      body: <p>The seat opens up again{upcoming ? ` and ${chip.name.split(' ')[0]} is notified` : ''}.</p>,
      confirm: 'Remove',
      danger: true,
    })
    if (!ok) return
    try {
      await send('delete', `/assignments/${chip.assignmentId}`)
      toast.success(`${chip.name} removed.`)
      announceChange()
      onChanged()
    } catch (e) {
      toast.error(errorMessage(e))
    }
  }

  const note = (
    <>
      <b style={{ color: 'var(--ink)' }}>{chip.name}</b>
      <br />
      {chip.isCourseTa ? 'Teaches this course. ' : ''}
      {chip.source === 'Manual' ? 'Assigned by hand.' : 'Assigned automatically.'}
      {chip.overrideReason && <><br />Override: {chip.overrideReason}</>}
      {chip.warnings.map((w) => (
        <span key={w} style={{ display: 'block', color: 'var(--short)', marginTop: 4 }}>
          {w}
        </span>
      ))}
    </>
  )

  return (
    <Menu
      align="left"
      note={note}
      items={[
        { label: 'Replace with the best available TA', icon: <IconSwap />, onClick: replace, hidden: !upcoming },
        { label: 'Open proctor profile', icon: <IconUsers />, onClick: () => navigate(`/proctors?ta=${chip.userId}`) },
        { label: 'Remove from this room', icon: <IconX />, onClick: remove, danger: true },
      ]}
      trigger={(toggle) => (
        <button className={cx('seat', chip.isCourseTa && 'course-ta', flagged && 'flagged')} onClick={toggle} title={chip.email}>
          {chip.isCourseTa && <IconFlag aria-label="Teaches this course" />}
          {flagged && <IconAlert aria-label="Breaks a rule" style={{ color: 'var(--warn)' }} />}
          <span>{chip.name}</span>
          {chip.source === 'Manual' && <span className="manual" title="Assigned by hand">M</span>}
        </button>
      )}
    />
  )
}
