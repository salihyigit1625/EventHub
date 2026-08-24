import type { EventDto } from '~/api-client'

export interface EventFacet {
  /** Etkinliğin en ucuz bilet tipi; bilet tipi yoksa null. */
  minPrice: number | null
  capacity: number
  remaining: number
  /** Satılan kontenjan yüzdesi (0-100). */
  percent: number
  soldOut: boolean
  lowStock: boolean
}

/** Liste uç noktası kontenjan/fiyat döndürmüyor, detay çağrıları sayfalar arasında paylaşılır. */
const facetCache = new Map<number, EventFacet>()

/** Vitrin sayfası tek seferde 12 kart gösterebiliyor, eşzamanlı istek sayısı sınırlanır. */
const CONCURRENCY = 4

/**
 * Kart üzerinde fiyat ve doluluk göstermek için etkinlik detaylarını
 * istemci tarafında, sayfa render'ını bloklamadan toplar.
 */
export function useEventFacets(ids: MaybeRefOrGetter<(number | undefined)[]>) {
  const api = useApi()
  const facets = ref<Record<number, EventFacet>>({})

  async function fetchFacet(id: number): Promise<EventFacet | null> {
    try {
      const event = await api.get<EventDto>(`/api/events/${id}`)
      let capacity = 0
      let remaining = 0
      let minPrice: number | null = null

      for (const type of event.ticketTypes ?? []) {
        capacity += type.totalQuantity ?? 0
        remaining += type.remainingQuantity ?? 0
        const price = type.price ?? 0
        if (minPrice === null || price < minPrice) minPrice = price
      }

      return {
        minPrice,
        capacity,
        remaining,
        percent: capacity ? Math.round(((capacity - remaining) / capacity) * 100) : 0,
        soldOut: capacity > 0 && remaining <= 0,
        lowStock: capacity > 0 && remaining > 0 && remaining / capacity <= 0.15
      }
    }
    catch {
      return null
    }
  }

  async function load() {
    const wanted = [...new Set(toValue(ids).filter((id): id is number => typeof id === 'number'))]
    if (!wanted.length) return

    const known: Record<number, EventFacet> = {}
    for (const id of wanted) {
      const hit = facetCache.get(id)
      if (hit) known[id] = hit
    }
    facets.value = known

    const queue = wanted.filter(id => !facetCache.has(id))
    if (!queue.length) return

    await Promise.all(
      Array.from({ length: Math.min(CONCURRENCY, queue.length) }, async () => {
        while (queue.length) {
          const id = queue.shift()
          if (id === undefined) return
          const facet = await fetchFacet(id)
          if (!facet) continue
          facetCache.set(id, facet)
          facets.value = { ...facets.value, [id]: facet }
        }
      })
    )
  }

  onMounted(() => {
    watch(() => toValue(ids).join(','), load, { immediate: true })
  })

  return { facets }
}
