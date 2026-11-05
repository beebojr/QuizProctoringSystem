import { useCallback, useEffect, useRef, useState } from 'react'
import type { Dispatch, SetStateAction } from 'react'
import { errorMessage } from '../api/client'

export interface QueryState<T> {
  data: T | undefined
  error: string | null
  loading: boolean
  reload: () => Promise<void>
  setData: Dispatch<SetStateAction<T | undefined>>
}

/** Loads data on mount and whenever deps change; ignores stale responses. */
export function useQuery<T>(load: () => Promise<T>, deps: unknown[], enabled = true): QueryState<T> {
  const [data, setData] = useState<T>()
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(enabled)
  const seq = useRef(0)
  const loadRef = useRef(load)
  loadRef.current = load

  const run = useCallback(async () => {
    const id = ++seq.current
    setLoading(true)
    try {
      const result = await loadRef.current()
      if (id === seq.current) {
        setData(result)
        setError(null)
      }
    } catch (e) {
      if (id === seq.current) setError(errorMessage(e))
    } finally {
      if (id === seq.current) setLoading(false)
    }
  }, [])

  useEffect(() => {
    if (!enabled) {
      setLoading(false)
      return
    }
    void run()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...deps, enabled])

  return { data, error, loading, reload: run, setData }
}

/** Closes a popover when clicking outside it or pressing Escape. */
export function useDismiss(open: boolean, close: () => void) {
  const ref = useRef<HTMLDivElement>(null)
  useEffect(() => {
    if (!open) return
    const onDown = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) close()
    }
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') close()
    }
    document.addEventListener('mousedown', onDown)
    document.addEventListener('keydown', onKey)
    return () => {
      document.removeEventListener('mousedown', onDown)
      document.removeEventListener('keydown', onKey)
    }
  }, [open, close])
  return ref
}

export function useLocalState<T>(key: string, initial: T): [T, (v: T) => void] {
  const [value, setValue] = useState<T>(() => {
    try {
      const raw = localStorage.getItem(key)
      return raw === null ? initial : (JSON.parse(raw) as T)
    } catch {
      return initial
    }
  })
  const set = useCallback(
    (v: T) => {
      setValue(v)
      try { localStorage.setItem(key, JSON.stringify(v)) } catch { /* ignore */ }
    },
    [key],
  )
  return [value, set]
}
