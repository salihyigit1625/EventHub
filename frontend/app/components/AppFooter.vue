<script setup lang="ts">
const year = new Date().getFullYear()
</script>

<template>
  <footer class="site-footer">
    <div class="site-footer__brand">
      <BrandLogo size="md" :link="false" />
      <p class="site-footer__tagline">Etkinlik keşfet, bilet al, kapıda okut.</p>
    </div>
    <nav class="site-footer__links" aria-label="Alt gezinme">
      <NuxtLink to="/">Keşfet</NuxtLink>
      <NuxtLink to="/register/organizer">Organizatör ol</NuxtLink>
      <NuxtLink to="/login">Giriş</NuxtLink>
    </nav>
    <p class="site-footer__legal">© {{ year }} EventHub</p>
  </footer>
</template>
