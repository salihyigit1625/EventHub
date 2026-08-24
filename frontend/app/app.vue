<script setup lang="ts">
const route = useRoute()

/** Kapı görevlisi kiosk modunda sayfa kaydırmasını kapatır. */
const isKiosk = computed(() => route.path.startsWith('/check-in'))

useHead({
  bodyAttrs: {
    class: computed(() => (isKiosk.value ? 'is-kiosk' : ''))
  }
})
</script>

<template>
  <div>
    <NuxtRouteAnnouncer />
    <NuxtLoadingIndicator color="#e8a653" :height="2" />
    <AppNotice />
    <NuxtLayout>
      <NuxtPage :transition="{ name: 'fade-slide', mode: 'out-in' }" />
    </NuxtLayout>
  </div>
</template>
