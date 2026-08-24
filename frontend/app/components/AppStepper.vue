<script setup lang="ts">
const props = defineProps<{
  steps: readonly string[]
  current: number
  /** Tamamlanmış adımlara geri dönülebilir. */
  maxReached?: number
}>()

const emit = defineEmits<{ go: [number] }>()

const reachable = computed(() => props.maxReached ?? props.current)

function go(index: number) {
  if (index <= reachable.value) emit('go', index)
}
</script>

<template>
  <nav class="stepper" aria-label="Adımlar">
    <button
      v-for="(step, index) in steps"
      :key="step"
      class="stepper__item"
      :class="{
        'stepper__item--active': index === current,
        'stepper__item--done': index < current
      }"
      type="button"
      :disabled="index > reachable"
      :aria-current="index === current ? 'step' : undefined"
      @click="go(index)"
    >
      <span class="stepper__num">{{ index < current ? '✓' : index + 1 }}</span>
      {{ step }}
    </button>
  </nav>
</template>
