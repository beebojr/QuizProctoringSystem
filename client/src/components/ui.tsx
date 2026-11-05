import { createContext, useCallback, useContext, useEffect, useId, useMemo, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import { createPortal } from 'react-dom'
import { useDismiss } from '../lib/hooks'
import { IconMore, IconSearch, IconX } from './Icons'

export function cx(...parts: (string | false | null | undefined)[]) {
  return parts.filter(Boolean).join(' ')
}

export function Spinner() {
  return <span className="spinner" role="status" aria-label="Loading" />
}

export function Loading() {
  return (
    <div className="loading">
      <Spinner />
    </div>
  )
}

export function Empty({ icon, title, children }: { icon?: ReactNode; title: string; children?: ReactNode }) {
  return (
    <div className="empty">
      {icon}
      <b>{title}</b>
      {children && <div>{children}</div>}
    </div>
  )
}

export function ErrorNote({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div className="notice short" role="alert">
      <span className="grow">{message}</span>
      {onRetry && (
        <button className="btn small" onClick={onRetry}>
          Try again
        </button>
      )}
    </div>
  )
}

export function Field({ label, hint, error, children, className }: { label: string; hint?: ReactNode; error?: string | null; children: ReactNode; className?: string }) {
  return (
    <label className={cx('field', error && 'err', className)}>
      <span>{label}</span>
      {children}
      {error ? <span className="error-text">{error}</span> : hint ? <small>{hint}</small> : null}
    </label>
  )
}

export function SearchBox({ value, onChange, placeholder, className }: { value: string; onChange: (v: string) => void; placeholder: string; className?: string }) {
  return (
    <div className={cx('search', className)}>
      <IconSearch />
      <input className="input" type="search" value={value} placeholder={placeholder} aria-label={placeholder} onChange={(e) => onChange(e.target.value)} />
    </div>
  )
}

export function Toggle({ checked, onChange, label, hint, disabled }: { checked: boolean; onChange: (v: boolean) => void; label?: ReactNode; hint?: ReactNode; disabled?: boolean }) {
  return (
    <label className="toggle">
      <input type="checkbox" role="switch" checked={checked} disabled={disabled} onChange={(e) => onChange(e.target.checked)} />
      <span className="track" />
      {(label || hint) && (
        <span className="toggle-text">
          {label && <b>{label}</b>}
          {hint && <small>{hint}</small>}
        </span>
      )}
    </label>
  )
}

export function Segmented<T extends string | number>({ value, options, onChange, dark, label }: { value: T; options: { value: T; label: ReactNode }[]; onChange: (v: T) => void; dark?: boolean; label?: string }) {
  return (
    <div className={cx('seg', dark && 'dark')} role="radiogroup" aria-label={label}>
      {options.map((o) => (
        <button key={String(o.value)} type="button" role="radio" aria-checked={o.value === value} className={cx(o.value === value && 'on')} onClick={() => onChange(o.value)}>
          {o.label}
        </button>
      ))}
    </div>
  )
}

export function Tabs<T extends string>({ value, tabs, onChange }: { value: T; tabs: { value: T; label: string; icon?: ReactNode }[]; onChange: (v: T) => void }) {
  return (
    <div className="tabs" role="tablist">
      {tabs.map((t) => (
        <button key={t.value} role="tab" aria-selected={t.value === value} className={cx(t.value === value && 'on')} onClick={() => onChange(t.value)}>
          {t.icon}
          {t.label}
        </button>
      ))}
    </div>
  )
}

interface ModalProps {
  title: ReactNode
  subtitle?: ReactNode
  onClose: () => void
  children: ReactNode
  footer?: ReactNode
  size?: 'normal' | 'wide' | 'xwide'
  icon?: ReactNode
}

export function Modal({ title, subtitle, onClose, children, footer, size = 'normal', icon }: ModalProps) {
  const titleId = useId()
  const panel = useRef<HTMLDivElement>(null)
  const closeRef = useRef(onClose)
  closeRef.current = onClose

  useEffect(() => {
    const previous = document.activeElement as HTMLElement | null
    const first = panel.current?.querySelector<HTMLElement>('input:not([type=hidden]), select, textarea, button.primary')
    ;(first ?? panel.current)?.focus()
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') closeRef.current()
    }
    document.addEventListener('keydown', onKey)
    const overflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    return () => {
      document.removeEventListener('keydown', onKey)
      document.body.style.overflow = overflow
      previous?.focus?.()
    }
  }, [])

  return createPortal(
    <div className="overlay" onMouseDown={(e) => e.target === e.currentTarget && onClose()}>
      <div className={cx('modal', size !== 'normal' && size)} role="dialog" aria-modal="true" aria-labelledby={titleId} ref={panel} tabIndex={-1}>
        <div className="modal-head">
          {icon}
          <div className="grow">
            <h2 id={titleId}>{title}</h2>
            {subtitle && <p>{subtitle}</p>}
          </div>
          <button className="icon-btn" onClick={onClose} aria-label="Close">
            <IconX />
          </button>
        </div>
        <div className="modal-body">{children}</div>
        {footer && <div className="modal-foot">{footer}</div>}
      </div>
    </div>,
    document.body,
  )
}

export interface MenuItem {
  label: string
  icon?: ReactNode
  onClick: () => void
  danger?: boolean
  hidden?: boolean
  divider?: boolean
}

/** A "more" button with a small action menu. */
export function Menu({ items, label = 'More actions', trigger, align = 'right', note }: { items: MenuItem[]; label?: string; trigger?: (open: () => void) => ReactNode; align?: 'left' | 'right'; note?: ReactNode }) {
  const [open, setOpen] = useState(false)
  const close = useCallback(() => setOpen(false), [])
  const ref = useDismiss(open, close)
  const visible = items.filter((i) => !i.hidden)
  return (
    <div ref={ref} style={{ position: 'relative', display: 'inline-flex' }}>
      {trigger ? (
        trigger(() => setOpen((o) => !o))
      ) : (
        <button className="icon-btn" aria-label={label} aria-haspopup="menu" aria-expanded={open} onClick={() => setOpen((o) => !o)}>
          <IconMore />
        </button>
      )}
      {open && (
        <div className="pop" role="menu" style={{ top: 'calc(100% + 4px)', [align]: 0 }}>
          {note && <div className="pop-note">{note}</div>}
          {visible.map((item) => (
            <div key={item.label}>
              {item.divider && <hr />}
              <button
                role="menuitem"
                className={cx(item.danger && 'danger')}
                onClick={() => {
                  setOpen(false)
                  item.onClick()
                }}
              >
                {item.icon}
                {item.label}
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}

// ── Confirmation dialog as a promise ─────────────────────────────────
interface ConfirmOptions {
  title: string
  body?: ReactNode
  confirm?: string
  danger?: boolean
}
const ConfirmContext = createContext<(o: ConfirmOptions) => Promise<boolean>>(async () => false)

export function ConfirmProvider({ children }: { children: ReactNode }) {
  const [pending, setPending] = useState<(ConfirmOptions & { resolve: (v: boolean) => void }) | null>(null)
  const ask = useCallback((o: ConfirmOptions) => new Promise<boolean>((resolve) => setPending({ ...o, resolve })), [])
  const done = (v: boolean) => {
    pending?.resolve(v)
    setPending(null)
  }
  return (
    <ConfirmContext.Provider value={ask}>
      {children}
      {pending && (
        <Modal
          title={pending.title}
          onClose={() => done(false)}
          footer={
            <>
              <button className="btn" onClick={() => done(false)}>
                Cancel
              </button>
              <button className={cx('btn', pending.danger ? 'danger solid' : 'primary')} onClick={() => done(true)} autoFocus>
                {pending.confirm ?? 'Confirm'}
              </button>
            </>
          }
        >
          {pending.body ?? null}
        </Modal>
      )}
    </ConfirmContext.Provider>
  )
}

export const useConfirm = () => useContext(ConfirmContext)

// ── Combobox: pick from a list or type a new value ───────────────────
export interface ComboOption {
  id: string
  label: string
  hint?: string
}

export function Combobox({
  value, options, onPick, onCreate, placeholder, createLabel = 'Add', inputClass, autoFocus,
}: {
  value: string
  options: ComboOption[]
  onPick: (o: ComboOption | null, text: string) => void
  onCreate?: (text: string) => void
  placeholder?: string
  createLabel?: string
  inputClass?: string
  autoFocus?: boolean
}) {
  const [text, setText] = useState(value)
  const [open, setOpen] = useState(false)
  const [active, setActive] = useState(0)
  const close = useCallback(() => setOpen(false), [])
  const ref = useDismiss(open, close)
  const listId = useId()

  useEffect(() => setText(value), [value])

  const matches = useMemo(() => {
    const q = text.trim().toLowerCase()
    const list = q ? options.filter((o) => o.label.toLowerCase().includes(q)) : options
    return list.slice(0, 60)
  }, [options, text])
  const exact = options.some((o) => o.label.toLowerCase() === text.trim().toLowerCase())
  const canCreate = !!onCreate && text.trim().length > 1 && !exact

  const choose = (o: ComboOption) => {
    setText(o.label)
    setOpen(false)
    onPick(o, o.label)
  }

  return (
    <div className="combo" ref={ref}>
      <input
        className={cx('input', inputClass)}
        value={text}
        placeholder={placeholder}
        autoFocus={autoFocus}
        role="combobox"
        aria-expanded={open}
        aria-controls={listId}
        aria-autocomplete="list"
        onFocus={() => setOpen(true)}
        onChange={(e) => {
          setText(e.target.value)
          setOpen(true)
          setActive(0)
          const hit = options.find((o) => o.label.toLowerCase() === e.target.value.trim().toLowerCase())
          onPick(hit ?? null, e.target.value)
        }}
        onKeyDown={(e) => {
          if (e.key === 'ArrowDown') {
            e.preventDefault()
            setOpen(true)
            setActive((a) => Math.min(a + 1, matches.length - 1))
          } else if (e.key === 'ArrowUp') {
            e.preventDefault()
            setActive((a) => Math.max(a - 1, 0))
          } else if (e.key === 'Enter' && open) {
            e.preventDefault()
            if (matches[active]) choose(matches[active])
            else if (canCreate) {
              onCreate?.(text.trim())
              setOpen(false)
            }
          } else if (e.key === 'Escape') setOpen(false)
        }}
      />
      {open && (matches.length > 0 || canCreate) && (
        <div className="combo-list" role="listbox" id={listId}>
          {matches.map((o, i) => (
            <button type="button" role="option" aria-selected={i === active} key={o.id} className={cx(i === active && 'active')} onMouseDown={(e) => e.preventDefault()} onClick={() => choose(o)}>
              <span>{o.label}</span>
              {o.hint && <span className="faint small">{o.hint}</span>}
            </button>
          ))}
          {canCreate && (
            <button type="button" className="create" onMouseDown={(e) => e.preventDefault()} onClick={() => { onCreate?.(text.trim()); setOpen(false) }}>
              {createLabel} “{text.trim()}”
            </button>
          )}
        </div>
      )}
    </div>
  )
}
