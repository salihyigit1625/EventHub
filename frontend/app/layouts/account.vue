<template>
  <div class="shell shell--lux theme-dark">
    <AppHeader />
    <div class="area-banner area-banner--lux">
      <div class="container">
        <p class="eyebrow">Katılımcı alanı</p>
        <h1 class="area-banner__title">Dijital cüzdanın</h1>
        <p class="area-banner__text">Biletler, bekleme sıran ve bakiyen tek yerde.</p>
        <nav class="subnav subnav--on-dark" aria-label="Hesap">
          <NuxtLink to="/account">Özet</NuxtLink>
          <NuxtLink to="/account/tickets">Biletlerim</NuxtLink>
          <NuxtLink to="/account/waitlist">Bekleme listesi</NuxtLink>
          <NuxtLink to="/account/wallet">Cüzdan</NuxtLink>
          <NuxtLink to="/account/payments">Ödemeler</NuxtLink>
        </nav>
      </div>
    </div>
    <main class="container page page--panel">
      <slot />
    </main>
    <div class="container">
      <AppFooter />
    </div>
  </div>
</template>
