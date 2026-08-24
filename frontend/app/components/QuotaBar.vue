<script setup lang="ts">
const props = withDefaults(
  defineProps<{
    remaining?: number | null
    total?: number | null
    label?: string
  }>(),
  { remaining: 0, total: 0, label: 'Doluluk' }
)

const total = computed(() => Math.max(0, props.total ?? 0))
const remaining = computed(() => Math.max(0, Math.min(props.remaining ?? 0, total.value)))
const sold = computed(() => total.value - remaining.value)

const percent = computed(() => (total.value === 0 ? 0 : Math.round((sold.value / total.value) * 100)))

const tone = computed(() => {
  if (total.value === 0) return ''
  if (remaining.value === 0) return 'quota--critical'
  if (remaining.value / total.value <= 0.15) return 'quota--warn'
  return ''
})
</script>

<template>
  <div class="quota" :class="tone">
    <div class="quota__legend">
      <span>{{ label }}</span>
      <span>%{{ percent }}</span>
    </div>
    <div
      class="quota__track"
      role="progressbar"
      :aria-valuenow="percent"
      aria-valuemin="0"
      aria-valuemax="100"
    >
      <div class="quota__fill" :style="{ width: `${percent}%` }" />
    </div>
    <div class="quota__legend">
      <span>{{ sold }} satıldı</span>
      <span>{{ remaining }} kaldı</span>
    </div>
  </div>
</template>
