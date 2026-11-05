import { useState } from 'react'
import type { FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { Mark } from '../components/Icons'
import { SlotRail } from '../components/SlotRail'
import { Field } from '../components/ui'
import { useAuth } from '../contexts/AuthContext'

const DEMO_RAILS = [
  { who: 'Laila', teaching: [{ start: '08:30', end: '10:00' }, { start: '13:45', end: '15:15' }] },
  { who: 'Omar', teaching: [{ start: '10:15', end: '11:45' }, { start: '12:00', end: '13:30' }] },
  { who: 'Nour', teaching: [{ start: '15:45', end: '17:15' }] },
]

export default function Login() {
  const { user, login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const from = (location.state as { from?: string } | null)?.from ?? '/'

  if (user) return <Navigate to={from} replace />

  const submit = async (e: FormEvent) => {
    e.preventDefault()
    if (!email.trim() || !password) {
      setError('Enter your email and password.')
      return
    }
    setBusy(true)
    setError(null)
    try {
      await login(email.trim(), password)
      navigate(from, { replace: true })
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Sign in failed.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="login">
      <section className="login-art" aria-hidden="true">
        <div className="brand" style={{ padding: 0 }}>
          <Mark className="brand-mark" />
          <div className="brand-name">Quiz Proctoring</div>
        </div>
        <h1>Every seat covered, nobody overbooked.</h1>
        <p>Quizzes are matched to teaching assistants who are free at that time, within two sessions a week and their semester limit.</p>
        <div className="login-rails">
          {DEMO_RAILS.map((r) => (
            <div key={r.who} className="lr">
              <span>{r.who}</span>
              <SlotRail teaching={r.teaching} window={{ start: '12:00', end: '13:30' }} label="" />
            </div>
          ))}
          <div className="lr">
            <span />
            <span>Nour and Laila are free for the 3rd-slot quiz; Omar is teaching.</span>
          </div>
        </div>
      </section>
      <section className="login-form">
        <form onSubmit={submit} noValidate>
          <h2>Sign in</h2>
          <p className="muted">Use your university email.</p>
          <Field label="Email">
            <input className="input" type="email" autoComplete="username" value={email} onChange={(e) => setEmail(e.target.value)} autoFocus />
          </Field>
          <Field label="Password">
            <input className="input" type="password" autoComplete="current-password" value={password} onChange={(e) => setPassword(e.target.value)} />
          </Field>
          {error && (
            <div className="notice short" role="alert">
              {error}
            </div>
          )}
          <button className="btn primary block" disabled={busy}>
            {busy ? 'Signing in…' : 'Sign in'}
          </button>
          {import.meta.env.DEV && (
            <div className="demo-logins">
              Demo data: <button type="button" onClick={() => { setEmail('admin@qps.com'); setPassword('password123') }}>admin@qps.com</button> or{' '}
              <button type="button" onClick={() => { setEmail('ahmed@qps.com'); setPassword('password123') }}>ahmed@qps.com</button>, password <b>password123</b>.
            </div>
          )}
        </form>
      </section>
    </div>
  )
}
