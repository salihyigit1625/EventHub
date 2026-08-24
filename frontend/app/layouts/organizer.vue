<template>
  <div class="shell shell--studio">
    <AppHeader />
    <div class="area-banner area-banner--studio">
      <div class="container">
        <p class="eyebrow">Organizatör stüdyosu</p>
        <h1 class="area-banner__title">Etkinlik yönetimi</h1>
        <p class="area-banner__text">Oluştur, afiş yükle, bilet tipi tanımla, yayınla.</p>
        <nav class="subnav" aria-label="Organizatör">
          <NuxtLink to="/organizer">Etkinliklerim</NuxtLink>
          <NuxtLink to="/organizer/events/new">Yeni etkinlik</NuxtLink>
          <NuxtLink to="/organizer/documents">Belgeler</NuxtLink>
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
