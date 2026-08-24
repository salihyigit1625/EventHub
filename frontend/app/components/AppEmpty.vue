<script setup lang="ts">
withDefaults(
  defineProps<{
    title: string
    text?: string
    icon?: string
  }>(),
  { text: '', icon: '◎' }
)
</script>

<template>
  <div class="empty-state">
    <div class="empty-state__icon" aria-hidden="true">{{ icon }}</div>
    <p class="empty-state__title">{{ title }}</p>
    <p v-if="text" class="empty-state__text">{{ text }}</p>
    <div v-if="$slots.default" style="margin-top: 0.35rem">
      <slot />
    </div>
  </div>
</template>
