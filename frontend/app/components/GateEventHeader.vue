<script setup lang="ts">
import type { EventDto, GateStaffProfileDto } from '~/api-client'

const props = withDefaults(
  defineProps<{
    profile?: GateStaffProfileDto | null
    scans?: number
    accepted?: number
  }>(),
  { profile: null, scans: 0, accepted: 0 }
)

const api = useApi()

const eventId = computed(() => props.profile?.assignedEventId ?? null)
const assigned = computed(() => Boolean(eventId.value))

/** Atama yalnız id + başlık döndürüyor; mekan ve saat için etkinliği çekiyoruz. */
const { data: event } = await useAsyncData<EventDto | null>(
  () => `gate-event-${eventId.value ?? 'none'}`,
  async () => {
    if (!eventId.value) return null
    try {
      return await api.get<EventDto>(`/api/events/${eventId.value}`)
    }
    catch {
      return null
    }
  },
  { watch: [eventId], lazy: true }
)

const title = computed(
  () => event.value?.title || props.profile?.assignedEventTitle || 'Etkinlik atanmadı'
)

const dayLabel = computed(() => {
  const raw = event.value?.startDate
  if (!raw) return null
  return new Intl.DateTimeFormat('tr-TR', {
    weekday: 'short',
    day: '2-digit',
    month: 'long'
  }).format(new Date(raw))
})

const timeLabel = computed(() => {
  const raw = event.value?.startDate
  if (!raw) return null
  return new Intl.DateTimeFormat('tr-TR', { hour: '2-digit', minute: '2-digit' })
    .format(new Date(raw))
})

const rejected = computed(() => Math.max(0, props.scans - props.accepted))
</script>

<template>
  <header class="gate-head" :class="{ 'gate-head--idle': !assigned }">
    <div class="gate-head__main">
      <p class="gate-head__label">
        <span class="gate-head__pulse" :class="{ 'gate-head__pulse--off': !assigned }" aria-hidden="true" />
        {{ assigned ? 'Atanan etkinlik' : 'Atama bekleniyor' }}
      </p>

      <h1 class="gate-head__title">{{ title }}</h1>

      <p v-if="assigned" class="gate-head__facts">
        <span v-if="event?.venue" class="gate-head__fact">
          <span aria-hidden="true">⌖</span>{{ event.venue }}
        </span>
        <span v-if="dayLabel" class="gate-head__fact">
          <span aria-hidden="true">▤</span>{{ dayLabel }}
        </span>
        <span v-if="timeLabel" class="gate-head__fact gate-head__fact--time">
          <span aria-hidden="true">◷</span>{{ timeLabel }}
        </span>
        <span class="gate-head__fact gate-head__fact--id">#{{ eventId }}</span>
      </p>
      <p v-else class="gate-head__facts">
        <span class="gate-head__fact">Yöneticinin seni bir etkinliğe ataması gerekiyor.</span>
      </p>
    </div>

    <dl class="gate-head__counters" aria-label="Oturum sayaçları">
      <div class="gate-counter">
        <dt>Okutma</dt>
        <dd>{{ scans }}</dd>
      </div>
      <div class="gate-counter gate-counter--ok">
        <dt>Kabul</dt>
        <dd>{{ accepted }}</dd>
      </div>
      <div class="gate-counter gate-counter--bad">
        <dt>Ret</dt>
        <dd>{{ rejected }}</dd>
      </div>
    </dl>
  </header>
</template>
