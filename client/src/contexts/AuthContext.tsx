import { createContext, useCallback, useContext, useEffect, useState } from 'react'
import type { ReactNode } from 'react'
import { api, errorMessage, get, tokenStore, unwrap } from '../api/client'
import type { AuthResponse, CurrentUser, Result } from '../api/types'

interface AuthState {
  user: CurrentUser | null
  ready: boolean
  login: (email: string, password: string) => Promise<CurrentUser>
  logout: () => void
  refresh: () => Promise<void>
}

const AuthContext = createContext<AuthState | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [ready, setReady] = useState(false)

  const refresh = useCallback(async () => {
    if (!tokenStore.get()) {
      setUser(null)
      return
    }
    try {
      setUser(await get<CurrentUser>('/auth/me'))
    } catch {
      tokenStore.clear()
      setUser(null)
    }
  }, [])

  useEffect(() => {
    refresh().finally(() => setReady(true))
    const onSignedOut = () => setUser(null)
    window.addEventListener('qps:signed-out', onSignedOut)
    return () => window.removeEventListener('qps:signed-out', onSignedOut)
  }, [refresh])

  const login = useCallback(async (email: string, password: string) => {
    try {
      const { data } = await api.post<Result<AuthResponse>>('/auth/login', { email, password })
      const auth = unwrap(data)
      tokenStore.set(auth.accessToken)
      setUser(auth.user)
      return auth.user
    } catch (e) {
      throw new Error(errorMessage(e, 'Sign in failed.'))
    }
  }, [])

  const logout = useCallback(() => {
    tokenStore.clear()
    setUser(null)
  }, [])

  return <AuthContext.Provider value={{ user, ready, login, logout, refresh }}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside AuthProvider')
  return ctx
}

/** For pages that only render after sign in. */
export function useUser(): CurrentUser {
  const { user } = useAuth()
  if (!user) throw new Error('Not signed in')
  return user
}
