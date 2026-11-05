import { useCallback, useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { get, send } from '../api/client'
import type { NotificationFeed, NotificationItem } from '../api/types'
import { useDismiss } from '../lib/hooks'
import { relativeTime } from '../lib/format'
import { IconBell } from './Icons'
import { cx } from './ui'

export function NotificationBell() {
  const [feed, setFeed] = useState<NotificationFeed>({ unreadCount: 0, items: [] })
  const [open, setOpen] = useState(false)
  const close = useCallback(() => setOpen(false), [])
  const ref = useDismiss(open, close)
  const navigate = useNavigate()

  const load = useCallback(async () => {
    try {
      setFeed(await get<NotificationFeed>('/notifications', { take: 40 }))
    } catch {
      /* the bell stays quiet when offline */
    }
  }, [])

  useEffect(() => {
    void load()
    const timer = window.setInterval(load, 60_000)
    window.addEventListener('qps:changed', load)
    return () => {
      window.clearInterval(timer)
      window.removeEventListener('qps:changed', load)
    }
  }, [load])

  const openItem = async (item: NotificationItem) => {
    if (!item.isRead) {
      setFeed((f) => ({ unreadCount: Math.max(0, f.unreadCount - 1), items: f.items.map((i) => (i.id === item.id ? { ...i, isRead: true } : i)) }))
      send('put', `/notifications/${item.id}/read`).catch(() => undefined)
    }
    if (item.link) {
      setOpen(false)
      navigate(item.link)
    }
  }

  const readAll = async () => {
    setFeed((f) => ({ unreadCount: 0, items: f.items.map((i) => ({ ...i, isRead: true })) }))
    await send('put', '/notifications/read-all').catch(() => undefined)
  }

  return (
    <div ref={ref} style={{ position: 'relative' }}>
      <button className="icon-btn" aria-label={`Notifications, ${feed.unreadCount} unread`} aria-expanded={open} onClick={() => { setOpen((o) => !o); if (!open) void load() }}>
        <IconBell />
        {feed.unreadCount > 0 && <span className="dot">{feed.unreadCount > 99 ? '99+' : feed.unreadCount}</span>}
      </button>
      {open && (
        <div className="pop bell-panel">
          <div className="bell-head">
            <h3>Notifications</h3>
            {feed.unreadCount > 0 && (
              <button className="btn ghost small" onClick={readAll}>
                Mark all as read
              </button>
            )}
          </div>
          <div className="bell-list">
            {feed.items.length === 0 && <div className="empty">Nothing new. Changes to quizzes, schedules and excuses show up here.</div>}
            {feed.items.map((item) => (
              <div key={item.id} className={cx('bell-item', !item.isRead && 'unread')} role="button" tabIndex={0} onClick={() => openItem(item)} onKeyDown={(e) => e.key === 'Enter' && openItem(item)}>
                <span className="pip" />
                <div className="grow">
                  <p>{item.message}</p>
                  <time>{relativeTime(item.createdAt)}</time>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}

/** Tells listeners (the bell, open pages) that data changed. */
export const announceChange = () => window.dispatchEvent(new Event('qps:changed'))
