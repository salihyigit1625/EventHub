<script setup lang="ts">
definePageMeta({ layout: 'organizer', middleware: ['organizer'] })

await navigateTo('/profile', { replace: true })
</script>

<template>
  <p class="event-card__meta">Profil sayfasına yönlendiriliyorsun…</p>
</template>
