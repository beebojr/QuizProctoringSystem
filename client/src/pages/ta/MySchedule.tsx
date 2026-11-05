import { useEffect, useState } from 'react'
import { get } from '../../api/client'
import type { Day, ScheduleSlot } from '../../api/types'
import { PageHead } from '../../components/AppShell'
import { IconLock } from '../../components/Icons'
import { ScheduleGrid, ScheduleLegend, SlotEditor } from '../../components/ScheduleGrid'
import { ErrorNote, Loading } from '../../components/ui'
import { useAuth, useUser } from '../../contexts/AuthContext'
import { useQuery } from '../../lib/hooks'
import { plural } from '../../lib/format'

export default function MySchedule() {
  const user = useUser()
  const { refresh } = useAuth()
  const slots = useQuery(() => get<ScheduleSlot[]>('/schedules'), [])
  const [editor, setEditor] = useState<{ slot?: ScheduleSlot; day?: Day; slotNumber?: number } | null>(null)

  // The lock can change while signed in; read it fresh.
  useEffect(() => {
    void refresh()
  }, [refresh])

  const editable = user.canEditSchedule
  return (
    <>
      <PageHead
        title="My teaching schedule"
        sub="You are never assigned to proctor while you teach. Keep this timetable complete and up to date."
      />
      {!editable && (
        <div className="notice" style={{ marginBottom: 16 }}>
          <IconLock />
          <span>An administrator has locked schedules. Ask them if something needs to change.</span>
        </div>
      )}
      <div className="spread" style={{ marginBottom: 12 }}>
        <ScheduleLegend />
        <span className="small muted">
          {plural(slots.data?.length ?? 0, 'class', 'classes')} a week{editable ? '. Click an empty cell to add a class.' : '.'}
        </span>
      </div>
      {slots.error && <ErrorNote message={slots.error} onRetry={slots.reload} />}
      {slots.data ? (
        <ScheduleGrid slots={slots.data} daysOff={user.daysOff} editable={editable} onAdd={(day, slotNumber) => setEditor({ day, slotNumber })} onEdit={(slot) => setEditor({ slot })} />
      ) : (
        <Loading />
      )}
      {editor && <SlotEditor {...editor} onClose={() => setEditor(null)} onSaved={slots.reload} />}
    </>
  )
}
