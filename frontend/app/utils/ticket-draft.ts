import type { TicketTypeDto } from '~/api-client'
import { ticketTypeFormSchema } from '~/schemas/events'
import type { TicketTypeFormInput } from '~/schemas/events'

/** Organizatör panelinde düzenlenen bilet tipi satırı. */
export interface TicketTypeDraft {
  /** Kaydedilmiş tipler API id'si taşır; taslak satırlar taşımaz. */
  id?: number
  key: string
  name: string
  price: number
  totalQuantity: number
  maxTicketsPerUser: number
  saleStartDate: string
  saleEndDate: string
  remainingQuantity?: number
  dirty: boolean
}

let seq = 0

export function nextDraftKey() {
  return `tt-${Date.now().toString(36)}-${++seq}`
}

export function blankTicketDraft(seed: Partial<TicketTypeDraft> = {}): TicketTypeDraft {
  return {
    key: nextDraftKey(),
    name: '',
    price: 0,
    totalQuantity: 50,
    maxTicketsPerUser: 4,
    saleStartDate: '',
    saleEndDate: '',
    dirty: true,
    ...seed
  }
}

export function ticketDraftFromDto(dto: TicketTypeDto): TicketTypeDraft {
  return {
    id: dto.id,
    key: `tt-${dto.id}`,
    name: dto.name ?? '',
    price: dto.price ?? 0,
    totalQuantity: dto.totalQuantity ?? 1,
    maxTicketsPerUser: dto.maxTicketsPerUser ?? 1,
    saleStartDate: isoToLocalInput(dto.saleStartDate),
    saleEndDate: isoToLocalInput(dto.saleEndDate),
    remainingQuantity: dto.remainingQuantity,
    dirty: false
  }
}

/**
 * Bir satırı doğrular. Bileşenden bağımsızdır: sihirbaz, satır düzenleyici
 * ekranda olmasa da aynı kuralları çalıştırabilsin diye burada durur.
 */
export function parseTicketDraft(row: TicketTypeDraft):
  { ok: true, data: TicketTypeFormInput } | { ok: false, message: string } {
  const result = ticketTypeFormSchema.safeParse({
    name: row.name,
    price: row.price,
    totalQuantity: row.totalQuantity,
    maxTicketsPerUser: row.maxTicketsPerUser,
    saleStartDate: row.saleStartDate,
    saleEndDate: row.saleEndDate
  })
  if (result.success) return { ok: true, data: result.data }
  return { ok: false, message: result.error.issues[0]?.message ?? 'Alanları kontrol et.' }
}

/** Satır anahtarına göre hata haritası döndürür; boş harita geçerli demektir. */
export function validateTicketDrafts(rows: TicketTypeDraft[]) {
  const issues: Record<string, string> = {}
  for (const row of rows) {
    const parsed = parseTicketDraft(row)
    if (!parsed.ok) issues[row.key] = parsed.message
  }
  return issues
}

/** Hata mesajını "hangi tip, ne eksik" biçiminde özetler. */
export function describeTicketIssues(rows: TicketTypeDraft[], issues: Record<string, string>) {
  const labelled = rows
    .filter(row => issues[row.key])
    .map(row => `${row.name?.trim() || 'Adsız tip'}: ${issues[row.key]}`)
  return labelled.join(' · ')
}

export const TICKET_TYPE_PRESETS = [
  { name: 'Erken Kayıt', price: 250, totalQuantity: 100, maxTicketsPerUser: 2 },
  { name: 'Standart', price: 450, totalQuantity: 300, maxTicketsPerUser: 4 },
  { name: 'VIP', price: 1200, totalQuantity: 40, maxTicketsPerUser: 2 }
] as const

/** VIP benzeri üst segment tipleri parlak kartla gösterilir. */
export function isPremiumTier(name?: string | null) {
  if (!name) return false
  return /vip|premium|platin|gold|altın|loca/i.test(name)
}
