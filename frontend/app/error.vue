<script setup lang="ts">
const error = useError()
</script>

<template>
  <div class="container page" style="max-width: 36rem">
    <BrandLogo size="md" />
    <p class="eyebrow" style="margin-top: 1.5rem">Hata</p>
    <h1>{{ error?.statusCode || 500 }}</h1>
    <p class="empty">{{ error?.statusMessage || 'Sayfa bulunamadı.' }}</p>
    <NuxtLink class="btn btn-primary" to="/">Ana sayfaya dön</NuxtLink>
  </div>
</template>
