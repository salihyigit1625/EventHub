<script setup lang="ts">
const props = withDefaults(
  defineProps<{
    to?: string | null
    pastLabel?: string
  }>(),
  { to: null, pastLabel: 'Etkinlik başladı' }
)

const { parts, isPast, isSoon } = useCountdown(() => props.to)

const cells = computed(() => [
  { value: parts.value.days, label: 'Gün' },
  { value: parts.value.hours, label: 'Saat' },
  { value: parts.value.minutes, label: 'Dakika' },
  { value: parts.value.seconds, label: 'Saniye' }
])

const pad = (n: number) => String(n).padStart(2, '0')
</script>

<template>
  <p v-if="!to" class="event-card__meta">Tarih belirtilmemiş.</p>
  <p v-else-if="isPast" class="badge badge-muted">{{ pastLabel }}</p>
  <div v-else class="countdown" :class="{ 'countdown--live': isSoon }">
    <div v-for="cell in cells" :key="cell.label" class="countdown__cell">
      <span class="countdown__num">{{ pad(cell.value) }}</span>
      <span class="countdown__label">{{ cell.label }}</span>
    </div>
  </div>
</template>
