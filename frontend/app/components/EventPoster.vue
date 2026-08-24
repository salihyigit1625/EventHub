<script setup lang="ts">
const props = withDefaults(defineProps<{
  eventId: number
  hasPoster?: boolean
  version?: number | string | null
}>(), {
  hasPoster: true,
  version: null
})

const api = useApi()
const broken = ref(false)

const src = computed(() => {
  const base = api.posterUrl(props.eventId)
  const stamp = props.version ?? '0'
  return `${base}?v=${encodeURIComponent(String(stamp))}`
})

watch(
  () => [props.eventId, props.version, props.hasPoster] as const,
  () => {
    broken.value = false
  }
)
</script>

<template>
  <figure class="poster">
    <img
      v-if="hasPoster && !broken"
      :key="src"
      :src="src"
      alt="Etkinlik afişi"
      @error="broken = true"
    >
    <div v-else class="poster-empty">Afiş henüz yüklenmedi</div>
  </figure>
</template>
