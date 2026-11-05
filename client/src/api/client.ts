import axios, { AxiosError } from 'axios'
import type { Result } from './types'

const TOKEN_KEY = 'qps.token'

export const tokenStore = {
  get: (): string | null => {
    try { return localStorage.getItem(TOKEN_KEY) } catch { return null }
  },
  set: (t: string) => {
    try { localStorage.setItem(TOKEN_KEY, t) } catch { /* private mode */ }
  },
  clear: () => {
    try { localStorage.removeItem(TOKEN_KEY) } catch { /* private mode */ }
  },
}

export const api = axios.create({ baseURL: '/api' })

api.interceptors.request.use((config) => {
  const token = tokenStore.get()
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  (r) => r,
  (error: AxiosError) => {
    const url = error.config?.url ?? ''
    if (error.response?.status === 401 && !url.includes('/auth/login')) {
      tokenStore.clear()
      window.dispatchEvent(new Event('qps:signed-out'))
    }
    return Promise.reject(error)
  },
)

/** Turns any failure into one readable sentence (server Result, validation errors, network). */
export function errorMessage(error: unknown, fallback = 'Something went wrong.'): string {
  if (axios.isAxiosError(error)) {
    if (!error.response) return 'The server is not reachable. Check that the API is running on port 3000.'
    if (error.response.status === 403) return 'Your account is not allowed to do that.'
    const data = error.response.data as { message?: string; errors?: string[] | Record<string, string[]>; title?: string } | undefined
    if (data && typeof data === 'object') {
      const list = Array.isArray(data.errors)
        ? data.errors
        : data.errors && typeof data.errors === 'object'
          ? Object.values(data.errors).flat()
          : []
      const message = data.message ?? data.title
      const extra = list.filter((e) => e && e !== message)
      if (message && extra.length) return `${message} ${extra.join(' ')}`
      if (message) return message
      if (extra.length) return extra.join(' ')
    }
    if (error.response.status === 404) return 'That item no longer exists.'
  }
  if (error instanceof Error && error.message) return error.message
  return fallback
}

/** Unwraps a Result<T> body, throwing its message when it failed. */
export function unwrap<T>(r: Result<T>): T {
  if (!r.isSuccess) throw new Error([r.message, ...(r.errors ?? [])].filter(Boolean).join(' ') || 'Request failed.')
  return r.value as T
}

export async function get<T>(url: string, params?: Record<string, unknown>): Promise<T> {
  const { data } = await api.get<T>(url, { params: clean(params) })
  return data
}

export async function send<T>(method: 'post' | 'put' | 'delete', url: string, body?: unknown): Promise<T> {
  const { data } = await api.request<Result<T>>({ method, url, data: body })
  return unwrap(data)
}

/** For endpoints that return a plain DTO rather than a Result. */
export async function post<T>(url: string, body?: unknown): Promise<T> {
  const { data } = await api.post<T>(url, body)
  return data
}

function clean(params?: Record<string, unknown>) {
  if (!params) return undefined
  const out: Record<string, unknown> = {}
  for (const [k, v] of Object.entries(params)) if (v !== undefined && v !== null && v !== '') out[k] = v
  return out
}

/** Downloads a file endpoint through the authenticated client. */
export async function download(url: string, params?: Record<string, unknown>, fallbackName = 'export.xlsx') {
  try {
    const res = await api.get<Blob>(url, { params: clean(params), responseType: 'blob' })
    const disposition = String(res.headers['content-disposition'] ?? '')
    const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(disposition)
    const name = match ? decodeURIComponent(match[1]) : fallbackName
    const href = URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = href
    a.download = name
    document.body.appendChild(a)
    a.click()
    a.remove()
    setTimeout(() => URL.revokeObjectURL(href), 1000)
  } catch (e) {
    // Blob error bodies need to be read as text to show the server's message.
    if (axios.isAxiosError(e) && e.response?.data instanceof Blob) {
      try {
        e.response.data = JSON.parse(await e.response.data.text())
      } catch { /* not JSON */ }
    }
    throw e
  }
}
