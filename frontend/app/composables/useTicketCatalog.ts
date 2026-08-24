import type { EventDto, PagedEventListItemDto } from '~/api-client'

export interface TicketContext {
  eventId?: number
  eventTitle?: string
  venue?: string
  startDate?: string
  typeName?: string
}

export interface CatalogIndex {
  types: Record<number, TicketContext>
  capacity: number
  sold: number
  events: number
}

/** Katalogdan çekilecek en fazla etkinlik sayısı. */
const INDEX_LIMIT = 24

/**
 * API bileti yalnız ticketTypeId ile döndürüyor; başlık/mekan bilgisi için
 * yayındaki etkinliklerden bir ticketTypeId → etkinlik dizini kurulur.
 */
export function useTicketCatalog() {
  const api = useApi()

  const empty = (): CatalogIndex => ({ types: {}, capacity: 0, sold: 0, events: 0 })

  const { data: index, pending } = useAsyncData<CatalogIndex>(
    'ticket-type-index',
    async () => {
      const result = empty()
      try {
        const list = await api.get<PagedEventListItemDto>('/api/events', {
          page: 1,
          pageSize: INDEX_LIMIT
        })
        const details = await Promise.all(
          (list.items ?? [])
            .slice(0, INDEX_LIMIT)
            .map(item =>
              item.id
                ? api.get<EventDto>(`/api/events/${item.id}`).catch(() => null)
                : Promise.resolve(null)
            )
        )
        for (const event of details) {
          if (!event) continue
          result.events += 1
          for (const type of event.ticketTypes ?? []) {
            const total = type.totalQuantity ?? 0
            result.capacity += total
            result.sold += total - (type.remainingQuantity ?? 0)
            if (type.id === undefined) continue
            result.types[type.id] = {
              eventId: event.id,
              eventTitle: event.title,
              venue: event.venue,
              startDate: event.startDate,
              typeName: type.name
            }
          }
        }
      }
      catch {
        // Dizin kurulamazsa biletler sade görünümde gösterilir.
      }
      return result
    },
    {
      default: empty,
      // Dizin 25 isteğe mal oluyor; sayfa gezintilerinde önbellekten okunur.
      getCachedData: (key, nuxtApp) =>
        (nuxtApp.payload.data[key] ?? nuxtApp.static.data[key]) as CatalogIndex | undefined
    }
  )

  function lookup(ticketTypeId?: number): TicketContext {
    if (ticketTypeId === undefined) return {}
    return index.value?.types?.[ticketTypeId] ?? {}
  }

  const occupancy = computed(() => {
    const capacity = index.value?.capacity ?? 0
    if (!capacity) return 0
    return Math.round(((index.value?.sold ?? 0) / capacity) * 100)
  })

  return { index, pending, lookup, occupancy }
}
