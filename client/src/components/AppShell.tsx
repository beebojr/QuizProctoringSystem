import { useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../contexts/AuthContext'
import { useTerm } from '../contexts/TermContext'
import { initials, longDate, todayIso } from '../lib/format'
import {
  IconBolt, IconCalendar, IconClock, IconExcuse, IconLock, IconLogout, IconMenu, IconOverview,
  IconPlus, IconReview, IconSettings, IconUpload, IconUsers, Mark,
} from './Icons'
import { NotificationBell } from './NotificationBell'
import { cx } from './ui'

interface NavItem {
  to: string
  label: string
  icon: ReactNode
  end?: boolean
}

const ADMIN_NAV: { group?: string; items: NavItem[] }[] = [
  { items: [{ to: '/', label: 'Overview', icon: <IconOverview />, end: true }] },
  {
    group: 'Exams',
    items: [
      { to: '/quizzes', label: 'Quizzes', icon: <IconCalendar />, end: true },
      { to: '/quizzes/new', label: 'Add quiz', icon: <IconPlus /> },
      { to: '/import', label: 'Import calendar', icon: <IconUpload /> },
      { to: '/assign', label: 'Assignment run', icon: <IconBolt /> },
    ],
  },
  {
    group: 'People',
    items: [
      { to: '/schedules', label: 'TA schedules', icon: <IconClock /> },
      { to: '/proctors', label: 'Proctors', icon: <IconUsers /> },
    ],
  },
  {
    group: 'Insight',
    items: [
      { to: '/reviewing', label: 'Reviewing', icon: <IconReview /> },
      { to: '/settings', label: 'Settings', icon: <IconSettings /> },
    ],
  },
]

const TA_NAV: { group?: string; items: NavItem[] }[] = [
  {
    items: [
      { to: '/', label: 'Home', icon: <IconOverview />, end: true },
      { to: '/my-assignments', label: 'My proctoring', icon: <IconCalendar /> },
      { to: '/my-schedule', label: 'My schedule', icon: <IconClock /> },
      { to: '/my-excuses', label: 'Excuses', icon: <IconExcuse /> },
      { to: '/account', label: 'Password', icon: <IconLock /> },
    ],
  },
]

export function AppShell() {
  const { user, logout } = useAuth()
  const { semesters, semester, setSemesterId } = useTerm()
  const [navOpen, setNavOpen] = useState(false)
  const [scrolled, setScrolled] = useState(false)
  const location = useLocation()
  const isAdmin = user?.role === 'Admin'
  const nav = isAdmin ? ADMIN_NAV : TA_NAV

  useEffect(() => setNavOpen(false), [location.pathname])
  useEffect(() => {
    const onScroll = () => setScrolled(window.scrollY > 4)
    window.addEventListener('scroll', onScroll, { passive: true })
    return () => window.removeEventListener('scroll', onScroll)
  }, [])

  if (!user) return null
  const total = semester?.totalWeeks ?? 0
  const current = semester?.currentWeek ?? 0

  return (
    <div className={cx('shell', navOpen && 'nav-open')}>
      <aside className="rail" aria-label="Main navigation">
        <div className="brand">
          <Mark className="brand-mark" />
          <div className="brand-name">
            Quiz Proctoring
            <small>{user.department} department</small>
          </div>
        </div>

        {semester && (
          <div className="term">
            {isAdmin && semesters.length > 1 ? (
              <select value={semester.id} onChange={(e) => setSemesterId(e.target.value)} aria-label="Semester">
                {semesters.map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.name}
                    {s.isActive ? '' : ' (inactive)'}
                  </option>
                ))}
              </select>
            ) : (
              <div className="term-name">{semester.name}</div>
            )}
            {total > 0 && (
              <div className="weekticks" style={{ gridTemplateColumns: `repeat(${total}, 1fr)` }} aria-hidden="true">
                {Array.from({ length: total }, (_, i) => (
                  <span key={i} className={cx(i + 1 < current && 'past', i + 1 === current && 'now')} />
                ))}
              </div>
            )}
            <div className="term-caption">
              {current >= 1 && current <= total ? (
                <>
                  Week <b>{current}</b> of {total}
                </>
              ) : current > total ? (
                'Semester has ended'
              ) : (
                'Semester has not started'
              )}
            </div>
          </div>
        )}

        <nav className="nav">
          {nav.map((section, i) => (
            <div key={i} style={{ display: 'contents' }}>
              {section.group && <div className="nav-group">{section.group}</div>}
              {section.items.map((item) => (
                <NavLink key={item.to} to={item.to} end={item.end} className={({ isActive }) => cx(isActive && 'active')}>
                  {item.icon}
                  {item.label}
                </NavLink>
              ))}
            </div>
          ))}
          {isAdmin && (
            <NavLink to="/account" className={({ isActive }) => cx(isActive && 'active')}>
              <IconLock />
              Password
            </NavLink>
          )}
        </nav>

        <div className="rail-foot">
          <span className="avatar">{initials(user.fullName)}</span>
          <div className="who">
            <b>{user.fullName}</b>
            <span>{isAdmin ? 'Administrator' : 'Teaching assistant'}</span>
          </div>
          <button onClick={logout} aria-label="Sign out" title="Sign out">
            <IconLogout width={18} height={18} />
          </button>
        </div>
      </aside>
      {navOpen && <div className="scrim" onClick={() => setNavOpen(false)} />}

      <div className="main">
        <header className={cx('topbar', scrolled && 'scrolled')}>
          <button className="icon-btn mobile-bar" aria-label="Open navigation" onClick={() => setNavOpen(true)}>
            <IconMenu />
          </button>
          <span className="today">{longDate(todayIso())}</span>
          <NotificationBell />
        </header>
        <main className="page">
          <Outlet />
        </main>
      </div>
    </div>
  )
}

export function PageHead({ title, sub, children }: { title: ReactNode; sub?: ReactNode; children?: ReactNode }) {
  return (
    <div className="page-head">
      <div>
        <h1>{title}</h1>
        {sub && <p>{sub}</p>}
      </div>
      {children && <div className="page-actions">{children}</div>}
    </div>
  )
}

