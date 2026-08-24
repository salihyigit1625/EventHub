<script setup lang="ts">
import type { EventListItemDto } from '~/api-client'
import type { EventFacet } from '~/composables/useEventFacets'

const props = withDefaults(
  defineProps<{
    event: EventListItemDto
    to?: string
    /** Vitrin sıralarında daha geniş, kapak ağırlıklı varyant. */
    featured?: boolean
    /** Fiyat ve doluluk bilgisi; yüklenene kadar boş bırakılabilir. */
    facet?: EventFacet | null
  }>(),
  { to: undefined, featured: false, facet: null }
)

const api = useApi()
const auth = useAuthStore()
const broken = ref(false)

const hasPoster = computed(() => Boolean(props.event.posterDocumentId))
const href = computed(() => props.to || `/events/${props.event.id}`)

const src = computed(() => {
  if (!props.event.id) return ''
  return `${api.posterUrl(props.event.id)}?v=${props.event.posterDocumentId ?? 0}`
})

const isManageLink = computed(() => props.to?.startsWith('/organizer'))

const ctaLabel = computed(() => {
  if (isManageLink.value) return 'Yönet'
  if (props.facet?.soldOut) return 'Tükendi'
  if (!auth.isAuthenticated) return 'İncele'
  if (auth.isAttendee) return 'Bilet al'
  return 'Görüntüle'
})

const startsSoon = computed(() => {
  if (!props.event.startDate) return false
  const diff = new Date(props.event.startDate).getTime() - Date.now()
  return diff > 0 && diff < 7 * 24 * 60 * 60 * 1000
})

/** Kapak üzerinde tek rozet gösterilir; aciliyet sırası tükendi → son biletler → bu hafta. */
const badge = computed(() => {
  if (props.facet?.soldOut) return { text: 'Tükendi', tone: 'out' as const }
  if (props.facet?.lowStock) return { text: 'Son biletler', tone: 'low' as const }
  if (startsSoon.value) return { text: 'Bu hafta', tone: 'soon' as const }
  return null
})

const priceLabel = computed(() => {
  const price = props.facet?.minPrice
  if (price === null || price === undefined) return ''
  if (price <= 0) return 'Ücretsiz'
  return `${formatMoney(price)}'den`
})

const timeLabel = computed(() => {
  if (!props.event.startDate) return ''
  return new Intl.DateTimeFormat('tr-TR', { hour: '2-digit', minute: '2-digit' })
    .format(new Date(props.event.startDate))
})

watch(() => props.event.posterDocumentId, () => {
  broken.value = false
})
</script>

<template>
  <NuxtLink
    class="event-card"
    :class="{
      'event-card--ready': auth.isAttendee,
      'event-card--featured': featured,
      'event-card--out': facet?.soldOut
    }"
    :to="href"
  >
    <div class="event-card__media">
      <img
        v-if="hasPoster && !broken"
        :key="src"
        :src="src"
        :alt="`${event.title} afişi`"
        loading="lazy"
        @error="broken = true"
      >
      <div v-else class="event-card__fallback" aria-hidden="true">
        <span>{{ (event.title || 'E').charAt(0).toLocaleUpperCase('tr') }}</span>
      </div>

      <div class="event-card__scrim" aria-hidden="true" />

      <div class="event-card__date" aria-hidden="true">
        <strong>{{ formatDay(event.startDate) }}</strong>
        <span>{{ formatMonth(event.startDate) }}</span>
      </div>

      <span v-if="badge" class="event-card__badge" :class="`event-card__badge--${badge.tone}`">
        {{ badge.text }}
      </span>

      <span v-if="priceLabel" class="event-card__price">{{ priceLabel }}</span>
      <span class="event-card__cta">{{ ctaLabel }}</span>
    </div>

    <div class="event-card__body">
      <h3 class="event-card__title">{{ event.title }}</h3>
      <p class="event-card__meta event-card__where">
        <span aria-hidden="true">⌖</span>
        {{ event.venue }}
      </p>
      <p class="event-card__meta event-card__when">
        {{ formatDateTime(event.startDate) }}
        <template v-if="timeLabel"> · {{ timeLabel }}</template>
      </p>

      <div v-if="facet && facet.capacity > 0" class="event-card__quota">
        <div class="event-card__quota-track">
          <span
            class="event-card__quota-fill"
            :class="{ 'event-card__quota-fill--hot': facet.percent >= 85 }"
            :style="{ width: `${Math.min(facet.percent, 100)}%` }"
          />
        </div>
        <span class="event-card__quota-label">
          <template v-if="facet.soldOut">Kontenjan doldu</template>
          <template v-else>%{{ facet.percent }} dolu · {{ facet.remaining }} bilet kaldı</template>
        </span>
      </div>
    </div>
  </NuxtLink>
</template>
