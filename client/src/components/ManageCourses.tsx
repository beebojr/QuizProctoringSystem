import { useMemo, useState } from 'react'
import toast from 'react-hot-toast'
import { errorMessage, get, send } from '../api/client'
import type { Course } from '../api/types'
import { useQuery } from '../lib/hooks'
import { plural } from '../lib/format'
import { IconCheck, IconEdit, IconPlus, IconTrash, IconX } from './Icons'
import { ErrorNote, Loading, Modal, SearchBox, useConfirm } from './ui'

export function ManageCourses({ onClose, department }: { onClose: () => void; department?: string }) {
  const courses = useQuery(() => get<Course[]>('/courses'), [])
  const confirm = useConfirm()
  const [search, setSearch] = useState('')
  const [name, setName] = useState('')
  const [code, setCode] = useState('')
  const [editing, setEditing] = useState<{ id: string; name: string; code: string } | null>(null)
  const [busy, setBusy] = useState(false)

  const list = useMemo(() => {
    const q = search.trim().toLowerCase()
    return (courses.data ?? []).filter((c) => !q || c.name.toLowerCase().includes(q) || (c.code ?? '').toLowerCase().includes(q))
  }, [courses.data, search])

  const add = async () => {
    if (name.trim().length < 2) return
    setBusy(true)
    try {
      const c = await send<Course>('post', '/courses', { name: name.trim(), code: code.trim() || null })
      courses.setData((l) => [...(l ?? []), c].sort((a, b) => a.name.localeCompare(b.name)))
      setName('')
      setCode('')
      toast.success(`${c.name} added.`)
    } catch (e) {
      toast.error(errorMessage(e))
    } finally {
      setBusy(false)
    }
  }

  const saveEdit = async () => {
    if (!editing || editing.name.trim().length < 2) return
    try {
      const c = await send<Course>('put', `/courses/${editing.id}`, { name: editing.name.trim(), code: editing.code.trim() || null })
      courses.setData((l) => l?.map((x) => (x.id === c.id ? c : x)))
      setEditing(null)
      toast.success('Course renamed.')
    } catch (e) {
      toast.error(errorMessage(e))
    }
  }

  const remove = async (c: Course) => {
    const ok = await confirm({
      title: `Delete ${c.name}?`,
      body: <p>{c.quizCount > 0 ? `It has ${plural(c.quizCount, 'quiz', 'quizzes')}, so it can only be removed after those quizzes are deleted.` : 'Teaching schedule entries keep their text but lose the link to this course.'}</p>,
      confirm: 'Delete course',
      danger: true,
    })
    if (!ok) return
    try {
      await send('delete', `/courses/${c.id}`)
      courses.setData((l) => l?.filter((x) => x.id !== c.id))
      toast.success('Course deleted.')
    } catch (e) {
      toast.error(errorMessage(e))
    }
  }

  return (
    <Modal
      title="Courses"
      subtitle={`${department ? department + ' department. ' : ''}${plural(courses.data?.length ?? 0, 'course')}. Names must match the quiz calendar so imports find them.`}
      onClose={onClose}
      size="wide"
      footer={
        <form className="row grow" onSubmit={(e) => { e.preventDefault(); void add() }}>
          <input className="input grow" placeholder="New course name" value={name} onChange={(e) => setName(e.target.value)} />
          <input className="input" style={{ width: 120 }} placeholder="Code" value={code} onChange={(e) => setCode(e.target.value)} />
          <button className="btn primary" disabled={busy || name.trim().length < 2}>
            <IconPlus />
            Add course
          </button>
        </form>
      }
    >
      <div className="stack">
        <SearchBox value={search} onChange={setSearch} placeholder="Search courses" />
        {courses.error && <ErrorNote message={courses.error} onRetry={courses.reload} />}
        {courses.loading && !courses.data && <Loading />}
        <div className="table-wrap">
          <table className="grid">
            <thead>
              <tr>
                <th>Course</th>
                <th>Code</th>
                <th className="num">Quizzes</th>
                <th className="num">TAs teaching</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {list.map((c) =>
                editing?.id === c.id ? (
                  <tr key={c.id}>
                    <td>
                      <input className="input small" value={editing.name} autoFocus onChange={(e) => setEditing({ ...editing, name: e.target.value })} onKeyDown={(e) => e.key === 'Enter' && saveEdit()} />
                    </td>
                    <td>
                      <input className="input small" style={{ width: 100 }} value={editing.code} onChange={(e) => setEditing({ ...editing, code: e.target.value })} onKeyDown={(e) => e.key === 'Enter' && saveEdit()} />
                    </td>
                    <td className="num">{c.quizCount}</td>
                    <td className="num">{c.taCount}</td>
                    <td className="nowrap" style={{ textAlign: 'right' }}>
                      <button className="icon-btn" aria-label="Save" onClick={saveEdit}><IconCheck /></button>
                      <button className="icon-btn" aria-label="Cancel" onClick={() => setEditing(null)}><IconX /></button>
                    </td>
                  </tr>
                ) : (
                  <tr key={c.id}>
                    <td><b style={{ fontWeight: 600 }}>{c.name}</b></td>
                    <td className="muted">{c.code ?? ''}</td>
                    <td className="num">{c.quizCount}</td>
                    <td className="num">{c.taCount}</td>
                    <td className="nowrap" style={{ textAlign: 'right' }}>
                      <button className="icon-btn" aria-label={`Rename ${c.name}`} onClick={() => setEditing({ id: c.id, name: c.name, code: c.code ?? '' })}><IconEdit /></button>
                      <button className="icon-btn" aria-label={`Delete ${c.name}`} onClick={() => remove(c)}><IconTrash /></button>
                    </td>
                  </tr>
                ),
              )}
            </tbody>
          </table>
        </div>
      </div>
    </Modal>
  )
}
