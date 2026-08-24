<script setup lang="ts">
const auth = useAuthStore()

const shellClass = computed(() => {
  if (!auth.isAuthenticated) return 'shell shell--home'
  if (auth.isAttendee) return 'shell shell--home shell--home-attendee'
  if (auth.isOrganizer) return 'shell shell--home shell--home-organizer'
  if (auth.isAdmin) return 'shell shell--home shell--home-admin'
  if (auth.isGateStaff) return 'shell shell--home shell--home-gate'
  return 'shell shell--home'
})
</script>

<template>
  <div :class="shellClass">
    <AppHeader />
    <main class="container page">
      <slot />
    </main>
    <div class="container">
      <AppFooter />
    </div>
  </div>
</template>
