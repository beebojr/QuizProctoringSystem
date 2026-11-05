import type { Assignment } from '../api/types'
import { longDate, prettySlot, shortDate, timeRange } from '../lib/format'
import { IconFlag, IconUsers } from './Icons'
import { cx } from './ui'

/** One proctoring session as seen by the TA. */
export function AssignmentRow({ a }: { a: Assignment }) {
  return (
    <div>
      <span className="when">
        <b>{shortDate(a.date)}</b>
        {timeRange(a.startTime, a.endTime)}
      </span>
      <span className="what">
        <b>
          {a.courseName}
          {a.examType !== 'Quiz' ? ` (${a.examType})` : ''}
        </b>
        <span>
          {a.isReserve ? 'Reserve, wait in the department office' : <>Room <span className="room">{a.roomName}</span>{a.group ? `, ${a.group}` : ''}</>}
          {a.coProctors.length > 0 && `. With ${a.coProctors.join(', ')}`}
        </span>
      </span>
      <span className="row">
        {a.isCourseTa && <span className="tag ta"><IconFlag />Your course</span>}
        <span className={cx('status', a.status)}>{a.status}</span>
      </span>
    </div>
  )
}

/** The next session, printed large like an exam-room slip. */
export function NextTicket({ a }: { a: Assignment }) {
  return (
    <div className="quiz" style={{ gridTemplateColumns: '120px minmax(0,1fr)' }}>
      <div className="quiz-time" style={{ background: 'var(--hl)', borderRightColor: '#d9b73f', justifyContent: 'center' }}>
        <span className="slot" style={{ fontSize: 26 }}>{prettySlot(a.slotLabel)}</span>
        <span className="t">{timeRange(a.startTime, a.endTime)}</span>
      </div>
      <div className="quiz-body stack-s" style={{ padding: '16px 18px' }}>
        <span className="small muted">Your next session, {longDate(a.date)}</span>
        <h2>{a.courseName}</h2>
        <div className="row-wrap">
          {a.isReserve ? (
            <span className="tag reserve">Reserve proctor</span>
          ) : (
            <span className="room" style={{ fontSize: 22 }}>{a.roomName}</span>
          )}
          {a.group && <span className="tag line">{a.group}</span>}
          <span className="small muted">Week {a.weekNumber}</span>
        </div>
        {a.coProctors.length > 0 && (
          <span className="small muted">
            <IconUsers width={14} height={14} style={{ verticalAlign: -2, marginRight: 4 }} />
            With {a.coProctors.join(', ')}
          </span>
        )}
      </div>
    </div>
  )
}
