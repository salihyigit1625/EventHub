const tr = new Intl.DateTimeFormat('tr-TR', {
  day: '2-digit',
  month: 'short',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit'
})

const trDay = new Intl.DateTimeFormat('tr-TR', { day: '2-digit' })
const trMonth = new Intl.DateTimeFormat('tr-TR', { month: 'short' })

const money = new Intl.NumberFormat('tr-TR', {
  style: 'currency',
  currency: 'TRY'
})

export function formatDateTime(value?: string | null) {
  if (!value) return '—'
  return tr.format(new Date(value))
}

export function formatDay(value?: string | null) {
  if (!value) return ''
  return trDay.format(new Date(value))
}

export function formatMonth(value?: string | null) {
  if (!value) return ''
  return trMonth.format(new Date(value)).replace('.', '').toUpperCase()
}

export function formatMoney(value?: number | null) {
  return money.format(value ?? 0)
}

export function isoToLocalInput(value?: string | null) {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

export function localInputToIso(value: string) {
  return new Date(value).toISOString()
}

export function formatBytes(value?: number | null) {
  const size = value ?? 0
  if (size < 1024) return `${size} B`
  if (size < 1024 * 1024) return `${(size / 1024).toFixed(1)} KB`
  return `${(size / (1024 * 1024)).toFixed(1)} MB`
}

export function issueDateLabel(date = new Date()) {
  return new Intl.DateTimeFormat('tr-TR', {
    weekday: 'long',
    day: '2-digit',
    month: 'long',
    year: 'numeric'
  }).format(date)
}
