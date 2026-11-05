// The semester being looked at (admins can switch; TAs always see the active one) and the rule settings.
import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { get } from '../api/client'
import type { Semester, Settings } from '../api/types'
import { useAuth } from './AuthContext'

interface TermState {
  semesters: Semester[]
  semester: Semester | null
  semesterId: string | undefined
  isActiveTerm: boolean
  setSemesterId: (id: string) => void
  settings: Settings | null
  reload: () => Promise<void>
  loaded: boolean
}

const TermContext = createContext<TermState | null>(null)
const KEY = 'qps.semester'

export function TermProvider({ children }: { children: ReactNode }) {
  const [semesters, setSemesters] = useState<Semester[]>([])
  const [settings, setSettings] = useState<Settings | null>(null)
  const [loaded, setLoaded] = useState(false)
  const [chosen, setChosen] = useState<string | null>(() => {
    try { return localStorage.getItem(KEY) } catch { return null }
  })

  const reload = useCallback(async () => {
    const [list, s] = await Promise.all([get<Semester[]>('/semesters'), get<Settings>('/settings')])
    setSemesters(list)
    setSettings(s)
    setLoaded(true)
  }, [])

  useEffect(() => {
    reload().catch(() => setLoaded(true))
  }, [reload])

  // Only admins browse other semesters; TAs always work in the active one.
  const { user } = useAuth()
  const canChoose = user?.role === 'Admin'
  const semester = useMemo(
    () => (canChoose ? semesters.find((s) => s.id === chosen) : undefined) ?? semesters.find((s) => s.isActive) ?? semesters[0] ?? null,
    [semesters, chosen, canChoose],
  )

  const setSemesterId = useCallback((id: string) => {
    setChosen(id)
    try { localStorage.setItem(KEY, id) } catch { /* ignore */ }
  }, [])

  const value: TermState = {
    semesters,
    semester,
    semesterId: semester?.id,
    isActiveTerm: !!semester?.isActive,
    setSemesterId,
    settings,
    reload,
    loaded,
  }
  return <TermContext.Provider value={value}>{children}</TermContext.Provider>
}

export function useTerm() {
  const ctx = useContext(TermContext)
  if (!ctx) throw new Error('useTerm must be used inside TermProvider')
  return ctx
}
