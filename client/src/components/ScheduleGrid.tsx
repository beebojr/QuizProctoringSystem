import { useState } from 'react'
import toast from 'react-hot-toast'
import { errorMessage, get, send } from '../api/client'
import type { Course, Day, ScheduleSlot, SlotType } from '../api/types'
import { useQuery } from '../lib/hooks'
import { SLOTS, TEACHING_DAYS, TEACHING_SLOTS, timeRange } from '../lib/format'
import { IconPlus, IconTrash } from './Icons'
import { announceChange } from './NotificationBell'
import { Combobox, Field, Modal, Segmented, cx, useConfirm } from './ui'

const gap = SLOTS.find((s) => s.number === 0)!

/** The weekly teaching timetable: Saturday to Thursday by the five slots. */
export function ScheduleGrid({ slots, daysOff, editable, onAdd, onEdit }: { slots: ScheduleSlot[]; daysOff: Day[]; editable: boolean; onAdd: (day: Day, slot: number) => void; onEdit: (s: ScheduleSlot) => void }) {
  return (
    <div className="table-wrap">
      <div className="week" style={{ minWidth: 680 }}>
        <div className="corner" />
        {TEACHING_DAYS.map((d) => (
          <div key={d} className={cx('dayname', daysOff.includes(d) && 'off')}>
            {d}
            {daysOff.includes(d) && <small>Day off</small>}
          </div>
        ))}
        {TEACHING_SLOTS.map((s) => (
          <Row key={s.number} slotNumber={s.number} slots={slots} daysOff={daysOff} editable={editable} onAdd={onAdd} onEdit={onEdit} withGapBefore={s.number === 5} />
        ))}
      </div>
    </div>
  )
}

function Row({ slotNumber, slots, daysOff, editable, onAdd, onEdit, withGapBefore }: { slotNumber: number; slots: ScheduleSlot[]; daysOff: Day[]; editable: boolean; onAdd: (d: Day, s: number) => void; onEdit: (s: ScheduleSlot) => void; withGapBefore: boolean }) {
  const def = SLOTS.find((s) => s.number === slotNumber)!
  return (
    <>
      {withGapBefore && (
        <div className="gaprow" style={{ gridColumn: '1 / -1' }}>
          Gap {timeRange(gap.start, gap.end)}, used for short quizzes
        </div>
      )}
      <div className="slotname">
        <b>{def.label}</b>
        <span>{timeRange(def.start, def.end)}</span>
      </div>
      {TEACHING_DAYS.map((day) => {
        const here = slots.filter((x) => x.day === day && x.slotNumber === slotNumber)
        const off = daysOff.includes(day)
        return (
          <div key={day} className={cx('cell', off && 'off')}>
            {here.map((x) => (
              <button key={x.id} type="button" className={cx('class', x.slotType)} onClick={() => onEdit(x)} disabled={!editable} title={editable ? 'Edit this class' : undefined} style={{ cursor: editable ? 'pointer' : 'default' }}>
                <b>{x.courseName}</b>
                <span>
                  <em style={{ fontStyle: 'normal' }}>{x.slotType}</em>
                  <span className="room">{x.room}</span>
                </span>
              </button>
            ))}
            {editable && here.length === 0 && (
              <button type="button" className="add-cell" aria-label={`Add a class on ${day}, ${def.name}`} onClick={() => onAdd(day, slotNumber)}>
                <IconPlus />
              </button>
            )}
          </div>
        )
      })}
    </>
  )
}

export function ScheduleLegend() {
  return (
    <div className="legend">
      <span><i style={{ background: 'var(--ta)' }} />Lab</span>
      <span><i style={{ background: 'var(--ok)' }} />Tutorial</span>
      <span><i style={{ background: 'var(--warn)' }} />Lecture</span>
      <span><i style={{ background: 'repeating-linear-gradient(135deg,#fff 0 3px,#d9ddd6 3px 5px)', border: '1px solid var(--line)' }} />Day off</span>
    </div>
  )
}

interface EditorProps {
  slot?: ScheduleSlot
  day?: Day
  slotNumber?: number
  userId?: string
  semesterId?: string
  onClose: () => void
  onSaved: () => void
}

/** Add, change or delete one class in a teaching schedule. */
export function SlotEditor({ slot, day: initialDay, slotNumber: initialSlot, userId, semesterId, onClose, onSaved }: EditorProps) {
  const courses = useQuery(() => get<Course[]>('/courses'), [])
  const confirm = useConfirm()
  const [day, setDay] = useState<Day>(slot?.day ?? initialDay ?? 'Saturday')
  const [slotNumber, setSlotNumber] = useState(slot?.slotNumber ?? initialSlot ?? 1)
  const [courseId, setCourseId] = useState<string | null>(slot?.courseId ?? null)
  const [courseName, setCourseName] = useState(slot?.courseName ?? '')
  const [slotType, setSlotType] = useState<SlotType>(slot?.slotType ?? 'Lab')
  const [room, setRoom] = useState(slot?.room ?? '')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const save = async () => {
    if (!courseName.trim()) {
      setError('Choose the course you teach in this slot.')
      return
    }
    if (!room.trim()) {
      setError('Enter the room of this class.')
      return
    }
    setBusy(true)
    setError(null)
    try {
      const body = { userId, semesterId, day, slotNumber, courseId, courseName: courseName.trim(), slotType, room: room.trim() }
      const result = slot
        ? await send<{ slot: ScheduleSlot | null; warnings: string[] }>('put', `/schedules/${slot.id}`, body)
        : await send<{ slot: ScheduleSlot | null; warnings: string[] }>('post', '/schedules', body)
      toast.success(slot ? 'Class updated.' : 'Class added.')
      result.warnings.forEach((w) => toast(w, { icon: '⚠️', duration: 8000 }))
      announceChange()
      onSaved()
      onClose()
    } catch (e) {
      setError(errorMessage(e))
    } finally {
      setBusy(false)
    }
  }

  const remove = async () => {
    if (!slot) return
    const ok = await confirm({ title: `Remove ${slot.courseName} on ${slot.day}?`, confirm: 'Remove class', danger: true })
    if (!ok) return
    try {
      await send('delete', `/schedules/${slot.id}`)
      toast.success('Class removed.')
      announceChange()
      onSaved()
      onClose()
    } catch (e) {
      toast.error(errorMessage(e))
    }
  }

  return (
    <Modal
      title={slot ? 'Edit class' : 'Add a class'}
      subtitle="Proctoring is never assigned during a class in the teaching schedule."
      onClose={onClose}
      footer={
        <>
          {slot && (
            <button className="btn danger" style={{ marginRight: 'auto' }} onClick={remove}>
              <IconTrash />
              Remove
            </button>
          )}
          <button className="btn" onClick={onClose}>
            Cancel
          </button>
          <button className="btn primary" onClick={save} disabled={busy}>
            {busy ? 'Saving…' : slot ? 'Save class' : 'Add class'}
          </button>
        </>
      }
    >
      <div className="form-grid">
        <Field label="Day">
          <select className="input" value={day} onChange={(e) => setDay(e.target.value as Day)}>
            {TEACHING_DAYS.map((d) => (
              <option key={d}>{d}</option>
            ))}
          </select>
        </Field>
        <Field label="Slot">
          <select className="input" value={slotNumber} onChange={(e) => setSlotNumber(Number(e.target.value))}>
            {TEACHING_SLOTS.map((s) => (
              <option key={s.number} value={s.number}>
                {s.name}, {timeRange(s.start, s.end)}
              </option>
            ))}
          </select>
        </Field>
        <Field label="Course" className="span-2">
          <Combobox
            value={courseName}
            placeholder="Search courses"
            options={(courses.data ?? []).map((c) => ({ id: c.id, label: c.name }))}
            onPick={(o, text) => {
              setCourseId(o?.id ?? null)
              setCourseName(text)
            }}
          />
        </Field>
        <Field label="Type">
          <Segmented value={slotType} onChange={setSlotType} options={(['Lab', 'Tut', 'Lecture'] as SlotType[]).map((t) => ({ value: t, label: t === 'Tut' ? 'Tutorial' : t }))} label="Class type" />
        </Field>
        <Field label="Room">
          <input className="input" value={room} placeholder="e.g. S1.107" onChange={(e) => setRoom(e.target.value)} />
        </Field>
      </div>
      {error && (
        <div className="notice short" style={{ marginTop: 14 }} role="alert">
          {error}
        </div>
      )}
    </Modal>
  )
}
