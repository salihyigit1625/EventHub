<script setup lang="ts">
defineProps<{
  label: string
  error?: string
}>()
</script>

<template>
  <label class="field">
    <span>{{ label }}</span>
    <slot />
    <small v-if="error" class="error">{{ error }}</small>
  </label>
</template>
