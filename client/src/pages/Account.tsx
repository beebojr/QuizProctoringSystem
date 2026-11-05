import { useState } from 'react'
import type { FormEvent } from 'react'
import toast from 'react-hot-toast'
import { errorMessage, send } from '../api/client'
import { PageHead } from '../components/AppShell'
import { Field } from '../components/ui'
import { useUser } from '../contexts/AuthContext'

export default function Account() {
  const user = useUser()
  const [current, setCurrent] = useState('')
  const [next, setNext] = useState('')
  const [again, setAgain] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    setError(null)
    if (!current) return setError('Enter your current password.')
    if (next.length < 8) return setError('The new password needs at least 8 characters.')
    if (next !== again) return setError('The two new passwords are different.')
    setBusy(true)
    try {
      await send('post', '/auth/change-password', { currentPassword: current, newPassword: next })
      toast.success('Password changed.')
      setCurrent('')
      setNext('')
      setAgain('')
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <PageHead title="Password" sub={`Signed in as ${user.email}.`} />
      <form className="sheet sheet-pad stack" style={{ maxWidth: 440 }} onSubmit={submit}>
        <Field label="Current password">
          <input className="input" type="password" autoComplete="current-password" value={current} onChange={(e) => setCurrent(e.target.value)} />
        </Field>
        <Field label="New password" hint="At least 8 characters.">
          <input className="input" type="password" autoComplete="new-password" value={next} onChange={(e) => setNext(e.target.value)} />
        </Field>
        <Field label="New password again">
          <input className="input" type="password" autoComplete="new-password" value={again} onChange={(e) => setAgain(e.target.value)} />
        </Field>
        {error && <div className="notice short" role="alert">{error}</div>}
        <button className="btn primary" disabled={busy}>{busy ? 'Changing…' : 'Change password'}</button>
      </form>
    </>
  )
}
