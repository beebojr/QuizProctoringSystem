import { useState } from 'react'
import toast from 'react-hot-toast'
import { errorMessage, get, send } from '../../api/client'
import type { Excuse } from '../../api/types'
import { PageHead } from '../../components/AppShell'
import { IconExcuse, IconPlus, IconTrash } from '../../components/Icons'
import { announceChange } from '../../components/NotificationBell'
import { ExcuseModal } from '../../components/PeopleForms'
import { Empty, ErrorNote, Loading, useConfirm } from '../../components/ui'
import { useQuery } from '../../lib/hooks'
import { fullDate } from '../../lib/format'

export default function MyExcuses() {
  const q = useQuery(() => get<Excuse[]>('/excuses'), [])
  const confirm = useConfirm()
  const [adding, setAdding] = useState(false)

  const withdraw = async (x: Excuse) => {
    const ok = await confirm({ title: 'Withdraw this excuse?', body: <p>You can be assigned again from {fullDate(x.startDate)} to {fullDate(x.endDate)}. Administrators are told.</p>, confirm: 'Withdraw', danger: true })
    if (!ok) return
    try {
      await send('delete', `/excuses/${x.id}`)
      toast.success('Excuse withdrawn.')
      announceChange()
      await q.reload()
    } catch (e) {
      toast.error(errorMessage(e))
    }
  }

  return (
    <>
      <PageHead title="Excuses" sub="Tell the administrators when you can't proctor. Nobody assigns you in an excused period.">
        <button className="btn primary" onClick={() => setAdding(true)}>
          <IconPlus />
          Submit an excuse
        </button>
      </PageHead>
      {q.error && <ErrorNote message={q.error} onRetry={q.reload} />}
      {!q.data ? (
        !q.error && <Loading />
      ) : q.data.length === 0 ? (
        <div className="sheet"><Empty icon={<IconExcuse />} title="No excuses">Submit one when you know you can't proctor on certain days.</Empty></div>
      ) : (
        <section className="sheet">
          <div className="table-wrap">
            <table className="grid">
              <thead>
                <tr>
                  <th>Period</th>
                  <th>Reason</th>
                  <th>Recorded by</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {q.data.map((x) => (
                  <tr key={x.id}>
                    <td className="nowrap">
                      <b style={{ fontWeight: 600 }}>{fullDate(x.startDate)}{x.endDate !== x.startDate ? ` – ${fullDate(x.endDate)}` : ''}</b>{' '}
                      {x.isActive ? <span className="tag warn">Active</span> : <span className="faint small">Past</span>}
                    </td>
                    <td>{x.reason}</td>
                    <td className="muted">{x.submittedByTa ? 'You' : 'An administrator'}</td>
                    <td style={{ textAlign: 'right' }}>
                      <button className="icon-btn" aria-label="Withdraw excuse" onClick={() => withdraw(x)}><IconTrash /></button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}
      {adding && <ExcuseModal isAdmin={false} onClose={() => setAdding(false)} onSaved={q.reload} />}
    </>
  )
}
