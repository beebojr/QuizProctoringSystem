import { useState } from 'react'
import { get } from '../../api/client'
import type { Assignment } from '../../api/types'
import { PageHead } from '../../components/AppShell'
import { AssignmentRow } from '../../components/AssignmentTicket'
import { IconCalendar } from '../../components/Icons'
import { Empty, ErrorNote, Loading, Segmented } from '../../components/ui'
import { useQuery } from '../../lib/hooks'
import { plural } from '../../lib/format'

export default function MyAssignments() {
  const q = useQuery(() => get<Assignment[]>('/assignments/my'), [])
  const [view, setView] = useState<'upcoming' | 'past'>('upcoming')
  if (q.error) return <ErrorNote message={q.error} onRetry={q.reload} />
  if (!q.data) return <Loading />
  const upcoming = q.data.filter((a) => a.status !== 'Finished').sort((a, b) => a.date.localeCompare(b.date) || a.startTime.localeCompare(b.startTime))
  const past = q.data.filter((a) => a.status === 'Finished').sort((a, b) => b.date.localeCompare(a.date) || b.startTime.localeCompare(a.startTime))
  const list = view === 'upcoming' ? upcoming : past
  return (
    <>
      <PageHead title="My proctoring" sub={`${plural(q.data.length, 'session')} this semester.`}>
        <Segmented value={view} onChange={setView} options={[{ value: 'upcoming', label: `Upcoming ${upcoming.length}` }, { value: 'past', label: `Done ${past.length}` }]} label="Show" dark />
      </PageHead>
      <section className="sheet">
        {list.length === 0 ? (
          <Empty icon={<IconCalendar />} title={view === 'upcoming' ? 'Nothing coming up' : 'No sessions done yet'} />
        ) : (
          <div className="sheet-pad history" style={{ paddingTop: 4, paddingBottom: 4 }}>
            {list.map((a) => <AssignmentRow key={a.assignmentId} a={a} />)}
          </div>
        )}
      </section>
    </>
  )
}
