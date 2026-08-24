<script setup lang="ts">
import type { WaitlistDto } from '~/api-client'

const props = defineProps<{ row: WaitlistDto }>()

const STAGES = ['Sıraya girdin', 'Sıra sana geldi', 'Bilete çevrildi'] as const

const stage = computed(() => {
  switch (props.row.status) {
    case WaitlistStatus.Notified: return 1
    case WaitlistStatus.Converted: return 2
    default: return 0
  }
})

const isReady = computed(() => props.row.status === WaitlistStatus.Notified)
const isClosed = computed(
  () => props.row.status === WaitlistStatus.Expired || props.row.status === WaitlistStatus.Converted
)

const percent = computed(() => {
  if (props.row.status === WaitlistStatus.Converted) return 100
  if (props.row.status === WaitlistStatus.Notified) return 100
  if (props.row.status === WaitlistStatus.Expired) return 0
  return 45
})

const headline = computed(() => {
  if (isReady.value) return 'Yer açıldı'
  if (props.row.status === WaitlistStatus.Converted) return 'Tamamlandı'
  if (props.row.status === WaitlistStatus.Expired) return 'Süre doldu'
  return 'Sırada'
})
</script>

<template>
  <div class="queue">
    <div
      class="ring"
      :class="{ 'ring--ready': isReady }"
      :style="{ '--value': percent }"
      role="img"
      :aria-label="`Bekleme durumu: ${WAITLIST_STATUS_LABEL[row.status ?? 0]}`"
    >
      <span class="ring__value">
        <strong>{{ isReady ? '★' : `${percent}%` }}</strong>
        <span>{{ headline }}</span>
      </span>
    </div>

    <div style="flex:1; min-width: 13rem; display:grid; gap:0.45rem">
      <div class="queue-steps">
        <template v-for="(label, index) in STAGES" :key="label">
          <span
            class="queue-steps__dot"
            :class="{
              'queue-steps__dot--done': index < stage,
              'queue-steps__dot--now': index === stage && !isClosed
            }"
          />
          <small :style="{ opacity: index === stage ? 1 : 0.55 }">{{ label }}</small>
        </template>
      </div>

      <p style="margin:0">
        <span class="status" :class="isReady ? 'status--ok' : isClosed ? 'status--bad' : 'status--warn'">
          {{ WAITLIST_STATUS_LABEL[row.status ?? 0] }}
        </span>
      </p>

      <p v-if="isReady && row.expiresAt" class="event-card__meta" style="margin:0">
        Hakkın {{ formatDateTime(row.expiresAt) }} tarihine kadar geçerli.
      </p>
      <p v-else class="event-card__meta" style="margin:0">
        Kayıt: {{ formatDateTime(row.requestedAt) }}
      </p>
    </div>
  </div>
</template>
