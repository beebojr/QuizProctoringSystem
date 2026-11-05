// A small hand-drawn stroke icon set (24px grid, 1.8 stroke) so the app has no icon dependency.
import type { ReactNode, SVGProps } from 'react'

type P = SVGProps<SVGSVGElement>
const base = (props: P, children: ReactNode) => (
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} strokeLinecap="round" strokeLinejoin="round" aria-hidden="true" {...props}>
    {children}
  </svg>
)

export const IconOverview = (p: P) => base(p, <><rect x="3.5" y="3.5" width="7" height="8" rx="1.5" /><rect x="13.5" y="3.5" width="7" height="5" rx="1.5" /><rect x="13.5" y="11.5" width="7" height="9" rx="1.5" /><rect x="3.5" y="14.5" width="7" height="6" rx="1.5" /></>)
export const IconCalendar = (p: P) => base(p, <><rect x="3.5" y="5" width="17" height="15.5" rx="2" /><path d="M3.5 9.5h17M8 3v4M16 3v4" /></>)
export const IconPlus = (p: P) => base(p, <path d="M12 5v14M5 12h14" />)
export const IconUpload = (p: P) => base(p, <><path d="M12 15V4M7.5 8.5 12 4l4.5 4.5" /><path d="M4 15v3.5A1.5 1.5 0 0 0 5.5 20h13a1.5 1.5 0 0 0 1.5-1.5V15" /></>)
export const IconDownload = (p: P) => base(p, <><path d="M12 4v11M7.5 10.5 12 15l4.5-4.5" /><path d="M4 15v3.5A1.5 1.5 0 0 0 5.5 20h13a1.5 1.5 0 0 0 1.5-1.5V15" /></>)
export const IconBolt = (p: P) => base(p, <path d="M13 3 5 13.5h6L10 21l8-10.5h-6L13 3Z" />)
export const IconClock = (p: P) => base(p, <><circle cx="12" cy="12" r="8.5" /><path d="M12 7.5V12l3 2" /></>)
export const IconUsers = (p: P) => base(p, <><circle cx="9" cy="8.5" r="3.5" /><path d="M2.5 20c.6-3.4 3.2-5.5 6.5-5.5s5.9 2.1 6.5 5.5" /><path d="M16 5.2a3.5 3.5 0 0 1 0 6.6M18 14.8c2 .7 3.2 2.6 3.5 5.2" /></>)
export const IconChart = (p: P) => base(p, <><path d="M4 20h16" /><path d="M7 16v-5M12 16V6M17 16v-8" /></>)
export const IconReview = (p: P) => base(p, <><rect x="5" y="4" width="14" height="17" rx="2" /><path d="M9 4.5V3h6v1.5M9 12l2 2 4-4.5" /></>)
export const IconSettings = (p: P) => base(p, <><circle cx="12" cy="12" r="3" /><path d="M19.4 15a1.7 1.7 0 0 0 .3 1.8l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-1.8-.3 1.7 1.7 0 0 0-1 1.5V21a2 2 0 1 1-4 0v-.1a1.7 1.7 0 0 0-1.1-1.5 1.7 1.7 0 0 0-1.8.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.7 1.7 0 0 0 .3-1.8 1.7 1.7 0 0 0-1.5-1H3a2 2 0 1 1 0-4h.1a1.7 1.7 0 0 0 1.5-1.1 1.7 1.7 0 0 0-.3-1.8l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.7 1.7 0 0 0 1.8.3H9a1.7 1.7 0 0 0 1-1.5V3a2 2 0 1 1 4 0v.1a1.7 1.7 0 0 0 1 1.5 1.7 1.7 0 0 0 1.8-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.7 1.7 0 0 0-.3 1.8V9a1.7 1.7 0 0 0 1.5 1H21a2 2 0 1 1 0 4h-.1a1.7 1.7 0 0 0-1.5 1Z" /></>)
export const IconBell = (p: P) => base(p, <><path d="M6 16V11a6 6 0 1 1 12 0v5l1.5 2h-15L6 16Z" /><path d="M10 20.5a2.2 2.2 0 0 0 4 0" /></>)
export const IconSearch = (p: P) => base(p, <><circle cx="11" cy="11" r="6.5" /><path d="m20 20-4.2-4.2" /></>)
export const IconChevronDown = (p: P) => base(p, <path d="m6 9 6 6 6-6" />)
export const IconChevronRight = (p: P) => base(p, <path d="m9 6 6 6-6 6" />)
export const IconChevronLeft = (p: P) => base(p, <path d="m15 6-6 6 6 6" />)
export const IconX = (p: P) => base(p, <path d="M6 6l12 12M18 6 6 18" />)
export const IconFlag = (p: P) => base(p, <><path d="M5 21V4" /><path d="M5 4h11l-2 4 2 4H5" /></>)
export const IconAlert = (p: P) => base(p, <><path d="M10.3 4.2 2.8 17.5A2 2 0 0 0 4.5 20.5h15a2 2 0 0 0 1.7-3L13.7 4.2a2 2 0 0 0-3.4 0Z" /><path d="M12 9.5v4M12 17h.01" /></>)
export const IconMore = (p: P) => base(p, <><circle cx="5.5" cy="12" r="1.2" fill="currentColor" /><circle cx="12" cy="12" r="1.2" fill="currentColor" /><circle cx="18.5" cy="12" r="1.2" fill="currentColor" /></>)
export const IconEdit = (p: P) => base(p, <><path d="M4 20h4L19 9a2.1 2.1 0 0 0-3-3L5 17v3Z" /><path d="m14 7 3 3" /></>)
export const IconTrash = (p: P) => base(p, <><path d="M4 7h16M10 11v6M14 11v6" /><path d="M6 7l1 12.5A1.5 1.5 0 0 0 8.5 21h7a1.5 1.5 0 0 0 1.5-1.5L18 7M9 7V4.5h6V7" /></>)
export const IconCopy = (p: P) => base(p, <><rect x="8.5" y="8.5" width="12" height="12" rx="2" /><path d="M15.5 8.5V5.5a2 2 0 0 0-2-2h-8a2 2 0 0 0-2 2v8a2 2 0 0 0 2 2h3" /></>)
export const IconSwap = (p: P) => base(p, <><path d="M4 8h14l-3.5-3.5M20 16H6l3.5 3.5" /></>)
export const IconUserPlus = (p: P) => base(p, <><circle cx="9" cy="8.5" r="3.5" /><path d="M2.5 20c.6-3.4 3.2-5.5 6.5-5.5 1.6 0 3 .5 4.2 1.4M18.5 13v6M15.5 16h6" /></>)
export const IconUserMinus = (p: P) => base(p, <><circle cx="9" cy="8.5" r="3.5" /><path d="M2.5 20c.6-3.4 3.2-5.5 6.5-5.5 1.6 0 3 .5 4.2 1.4M15.5 16h6" /></>)
export const IconLogout = (p: P) => base(p, <><path d="M14 4h4.5A1.5 1.5 0 0 1 20 5.5v13a1.5 1.5 0 0 1-1.5 1.5H14" /><path d="M10 16.5 5.5 12 10 7.5M5.5 12H15" /></>)
export const IconMenu = (p: P) => base(p, <path d="M4 7h16M4 12h16M4 17h16" />)
export const IconCheck = (p: P) => base(p, <path d="m5 12.5 4.5 4.5L19 7.5" />)
export const IconFile = (p: P) => base(p, <><path d="M14 3.5H7A1.5 1.5 0 0 0 5.5 5v14A1.5 1.5 0 0 0 7 20.5h10a1.5 1.5 0 0 0 1.5-1.5V8L14 3.5Z" /><path d="M14 3.5V8h4.5M9 13h6M9 16.5h4" /></>)
export const IconBook = (p: P) => base(p, <><path d="M4 5.5A1.5 1.5 0 0 1 5.5 4H11v16H5.5A1.5 1.5 0 0 1 4 18.5v-13ZM20 5.5A1.5 1.5 0 0 0 18.5 4H13v16h5.5a1.5 1.5 0 0 0 1.5-1.5v-13Z" /></>)
export const IconInfo = (p: P) => base(p, <><circle cx="12" cy="12" r="8.5" /><path d="M12 11v5M12 8h.01" /></>)
export const IconLock = (p: P) => base(p, <><rect x="5" y="10.5" width="14" height="10" rx="2" /><path d="M8.5 10.5V7.5a3.5 3.5 0 0 1 7 0v3" /></>)
export const IconExcuse = (p: P) => base(p, <><rect x="3.5" y="5" width="17" height="15.5" rx="2" /><path d="M3.5 9.5h17M8 3v4M16 3v4M9.5 13l5 5M14.5 13l-5 5" /></>)
export const IconRoom = (p: P) => base(p, <><path d="M4 20.5V4.5A1.5 1.5 0 0 1 5.5 3h9A1.5 1.5 0 0 1 16 4.5v16M2.5 20.5h19M16 8h3.5v12.5" /><path d="M12.5 12v1" /></>)
export const IconRefresh = (p: P) => base(p, <><path d="M19.5 12a7.5 7.5 0 1 1-2.2-5.3L19.5 9" /><path d="M19.5 4v5h-5" /></>)
export const IconSeat = (p: P) => base(p, <><path d="M6 11V5.5A1.5 1.5 0 0 1 7.5 4h9A1.5 1.5 0 0 1 18 5.5V11" /><path d="M4.5 11h15v4.5h-15zM6.5 15.5V20M17.5 15.5V20" /></>)

/** The product mark: a slot rail with the quiz window highlighted. */
export const Mark = (p: P) => (
  <svg viewBox="0 0 32 32" aria-hidden="true" {...p}>
    <rect width="32" height="32" rx="7" fill="#F4D35E" />
    <rect x="5" y="14" width="22" height="4" rx="2" fill="#15212B" opacity="0.35" />
    <rect x="14" y="12" width="7" height="8" rx="1.5" fill="#15212B" />
    <rect x="9" y="8" width="2" height="16" rx="1" fill="#15212B" />
  </svg>
)
