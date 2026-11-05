// Reads the university quiz calendar from a PDF (an Excel sheet printed to PDF).
//
// The calendar merges WEEK, DATE, SUBJECT and SLOT cells over the rows of every student
// group, and a merged cell's text is printed once, vertically centred. To give every group
// row its own date/subject/slot we rebuild the cell grid from the drawn borders: for each
// column we collect the horizontal border lines that cross it, so a merged cell is simply
// the band between two borders. When a PDF has no borders we fall back to the nearest text.

export interface RawRow {
  rowNumber: number
  week?: string
  date?: string
  subject?: string
  slot?: string
  group?: string
  location?: string
}

export interface TextItem {
  text: string
  x: number // left, in PDF units
  y: number // baseline, PDF units (origin bottom-left)
  width: number
  height: number
}

export interface Rect {
  x: number
  y: number
  w: number
  h: number
  dark: boolean
}

export interface PageData {
  items: TextItem[]
  rects: Rect[]
}

type ColumnKey = 'week' | 'date' | 'subject' | 'slot' | 'group' | 'location'

const HEADER_MATCHERS: Record<ColumnKey, (t: string) => boolean> = {
  week: (t) => t === 'week',
  date: (t) => t === 'date',
  subject: (t) => t === 'subject' || t === 'course',
  slot: (t) => t === 'slot',
  group: (t) => t === 'gp' || t === 'group',
  location: (t) => t === 'location' || t === 'room' || t === 'rooms',
}

interface Column {
  key: ColumnKey
  center: number
  left: number
  right: number
}

interface Cell {
  text: string
  top: number // larger y = higher on the page
  bottom: number
}

const centerY = (i: TextItem) => i.y + i.height / 2

function findHeader(items: TextItem[]): { y: number; columns: Column[] } | null {
  const byKey = new Map<ColumnKey, TextItem>()
  for (const item of items) {
    const t = item.text.trim().toLowerCase()
    for (const key of Object.keys(HEADER_MATCHERS) as ColumnKey[]) {
      if (!byKey.has(key) && HEADER_MATCHERS[key](t)) byKey.set(key, item)
    }
  }
  const date = byKey.get('date')
  const location = byKey.get('location')
  if (!date || !location || !byKey.get('subject')) return null

  // Header words must sit on (roughly) the same line as DATE.
  const headerY = centerY(date)
  const found = [...byKey.entries()]
    .filter(([, item]) => Math.abs(centerY(item) - headerY) < 4)
    .map(([key, item]) => ({ key, center: item.x + item.width / 2 }))
    .sort((a, b) => a.center - b.center)

  const columns: Column[] = found.map((c, i) => ({
    key: c.key,
    center: c.center,
    left: i === 0 ? -Infinity : (found[i - 1].center + c.center) / 2,
    right: i === found.length - 1 ? Infinity : (c.center + found[i + 1].center) / 2,
  }))
  return { y: headerY, columns }
}

/** Groups the text pieces that belong to one column into lines. */
function linesIn(items: TextItem[], col: Column): Cell[] {
  const inside = items
    .filter((i) => {
      const cx = i.x + i.width / 2
      return cx >= col.left && cx < col.right
    })
    .sort((a, b) => centerY(b) - centerY(a) || a.x - b.x)

  const lines: { y: number; parts: TextItem[] }[] = []
  for (const item of inside) {
    const line = lines.find((l) => Math.abs(l.y - centerY(item)) < Math.max(1.5, item.height * 0.45))
    if (line) line.parts.push(item)
    else lines.push({ y: centerY(item), parts: [item] })
  }
  return lines.map((l) => ({
    text: l.parts.sort((a, b) => a.x - b.x).map((p) => p.text).join(' ').replace(/\s+/g, ' ').trim(),
    top: l.y,
    bottom: l.y,
  }))
}

/** Y positions of the horizontal borders that cross a column (sorted top → bottom). */
function bordersFor(rects: Rect[], col: Column, below: number): number[] {
  const ys: number[] = []
  for (const r of rects) {
    const crosses = r.x <= col.center && r.x + r.w >= col.center
    if (!crosses) continue
    if (r.y > below) continue
    if (r.h <= 1.6 && r.w >= 6) ys.push(r.y + r.h / 2) // a border line
    else if (r.dark && r.h > 1.6) ys.push(r.y, r.y + r.h) // a black separator bar
  }
  ys.sort((a, b) => b - a)
  const merged: number[] = []
  for (const y of ys) if (!merged.length || Math.abs(merged[merged.length - 1] - y) > 0.9) merged.push(y)
  return merged
}

/** Joins text lines into cells using the column's borders (handles wrapped text too). */
function cellsFor(lines: Cell[], borders: number[]): Cell[] {
  if (borders.length < 2) return lines
  const cells: Cell[] = []
  for (let i = 0; i < borders.length - 1; i++) {
    const top = borders[i]
    const bottom = borders[i + 1]
    if (top - bottom < 2) continue
    const inside = lines.filter((l) => l.top <= top && l.top >= bottom)
    if (!inside.length) continue
    cells.push({ text: inside.map((l) => l.text).join(' '), top, bottom })
  }
  return cells
}

function cellAt(cells: Cell[], y: number, bordered: boolean): string | undefined {
  if (!cells.length) return undefined
  if (bordered) {
    const hit = cells.find((c) => y <= c.top + 0.5 && y >= c.bottom - 0.5)
    return hit?.text
  }
  // No borders: the nearest line above or level with the row (merged text is centred).
  let best: Cell | undefined
  let bestDistance = Infinity
  for (const c of cells) {
    const d = Math.abs(c.top - y)
    if (d < bestDistance) {
      best = c
      bestDistance = d
    }
  }
  return best?.text
}

/** Pure part: turns positioned text + rectangles into calendar rows. */
export function extractRows(pages: PageData[]): RawRow[] {
  const rows: RawRow[] = []
  let carryWeek: string | undefined
  let carryDate: string | undefined
  let rowNumber = 0

  for (const page of pages) {
    const header = findHeader(page.items)
    if (!header) continue
    const body = page.items.filter((i) => centerY(i) < header.y - 2)

    const perColumn = new Map<ColumnKey, { cells: Cell[]; bordered: boolean }>()
    for (const col of header.columns) {
      const lines = linesIn(body, col)
      const borders = bordersFor(page.rects, col, header.y)
      const bordered = borders.length >= 2
      perColumn.set(col.key, { cells: bordered ? cellsFor(lines, borders) : lines, bordered })
    }

    const locations = perColumn.get('location')
    if (!locations) continue
    const groupCol = perColumn.get('group')

    for (const loc of locations.cells) {
      const y = (loc.top + loc.bottom) / 2
      const get = (key: ColumnKey) => {
        const col = perColumn.get(key)
        return col ? cellAt(col.cells, y, col.bordered) : undefined
      }
      const week = get('week') ?? carryWeek
      const date = get('date') ?? carryDate
      if (week) carryWeek = week
      if (date) carryDate = date
      rowNumber++
      rows.push({
        rowNumber,
        week,
        date,
        subject: get('subject'),
        slot: get('slot'),
        group: groupCol ? cellAt(groupCol.cells, y, groupCol.bordered) : undefined,
        location: loc.text,
      })
    }
  }
  return rows
}

/** Loads a PDF in the browser with pdf.js and extracts the calendar rows. */
export async function readPdfCalendar(file: File): Promise<RawRow[]> {
  const pdfjs = await import('pdfjs-dist')
  const worker = await import('pdfjs-dist/build/pdf.worker.min.mjs?url')
  pdfjs.GlobalWorkerOptions.workerSrc = worker.default

  const data = new Uint8Array(await file.arrayBuffer())
  const doc = await pdfjs.getDocument({ data }).promise
  const pages: PageData[] = []
  for (let p = 1; p <= doc.numPages; p++) {
    const page = await doc.getPage(p)
    pages.push(await pageData(page, pdfjs.OPS))
  }
  return extractRows(pages)
}

interface PdfPage {
  getTextContent(): Promise<{ items: unknown[] }>
  getOperatorList(): Promise<{ fnArray: number[]; argsArray: unknown[][] }>
}

/** Text items and filled rectangles (borders, fills) of one page. */
export async function pageData(page: PdfPage, OPS: Record<string, number>): Promise<PageData> {
  const content = await page.getTextContent()
  const items: TextItem[] = []
  for (const raw of content.items as { str?: string; transform?: number[]; width?: number; height?: number }[]) {
    if (!raw.str || !raw.str.trim() || !raw.transform) continue
    items.push({ text: raw.str, x: raw.transform[4], y: raw.transform[5], width: raw.width ?? 0, height: raw.height ?? 0 })
  }

  const ops = await page.getOperatorList()
  const rects: Rect[] = []
  let fill: number[] = [0, 0, 0]
  const stack: number[][] = []
  let pending: [number, number, number, number][] = []
  for (let i = 0; i < ops.fnArray.length; i++) {
    const fn = ops.fnArray[i]
    const args = ops.argsArray[i] as unknown[]
    if (fn === OPS.save) stack.push(fill)
    else if (fn === OPS.restore) fill = stack.pop() ?? fill
    else if (fn === OPS.setFillRGBColor) fill = toRgb(args)
    else if (fn === OPS.setFillGray) fill = [Number(args[0]) * 255, Number(args[0]) * 255, Number(args[0]) * 255]
    else if (fn === OPS.constructPath) {
      pending = []
      const kinds = args[0] as number[]
      const coords = args[1] as ArrayLike<number>
      let k = 0
      for (const kind of kinds) {
        if (kind === OPS.rectangle) {
          pending.push([coords[k], coords[k + 1], coords[k + 2], coords[k + 3]])
          k += 4
        } else if (kind === OPS.moveTo || kind === OPS.lineTo) k += 2
        else if (kind === OPS.curveTo) k += 6
        else if (kind === OPS.curveTo2 || kind === OPS.curveTo3) k += 4
      }
    } else if (fn === OPS.fill || fn === OPS.eoFill || fn === OPS.fillStroke || fn === OPS.eoFillStroke) {
      const dark = fill[0] + fill[1] + fill[2] < 120
      for (const [x, y, w, h] of pending) rects.push(normalize(x, y, w, h, dark))
      pending = []
    } else if (fn === OPS.endPath || fn === OPS.clip || fn === OPS.eoClip) {
      if (fn === OPS.endPath) pending = []
    }
  }
  return { items, rects }
}

function toRgb(args: unknown[]): number[] {
  const first = args[0]
  if (typeof first === 'string' && first.startsWith('#')) {
    const n = parseInt(first.slice(1), 16)
    return [(n >> 16) & 255, (n >> 8) & 255, n & 255]
  }
  return [Number(args[0]), Number(args[1]), Number(args[2])]
}

function normalize(x: number, y: number, w: number, h: number, dark: boolean): Rect {
  return { x: w < 0 ? x + w : x, y: h < 0 ? y + h : y, w: Math.abs(w), h: Math.abs(h), dark }
}
