<script setup lang="ts">
const props = withDefaults(
  defineProps<{
    wordmark?: boolean
    size?: 'sm' | 'md' | 'lg'
    link?: boolean
    /** Belirtilmezse yalnız büyük boyutta gösterilir. */
    tagline?: boolean
  }>(),
  { wordmark: true, size: 'sm', link: true, tagline: undefined }
)

const NuxtLinkComponent = resolveComponent('NuxtLink')

const root = computed(() => (props.link ? NuxtLinkComponent : 'div'))
const rootProps = computed(() => (props.link ? { to: '/' } : {}))
const showTagline = computed(() => props.tagline ?? props.size === 'lg')
</script>

<template>
  <component :is="root" class="brand" :class="`brand--${size}`" v-bind="rootProps">
    <span class="brand-mark" aria-hidden="true">
      <img src="/brand/eventhub-mark.png" alt="" width="64" height="64" decoding="async">
    </span>
    <span v-if="wordmark" class="brand-word">
      <span class="brand-word__name">EventHub</span>
      <span v-if="showTagline" class="brand-word__tag">Etkinlik keşfet · bilet al</span>
    </span>
  </component>
</template>
