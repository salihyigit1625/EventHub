<template>
  <div class="shell shell--console theme-dark">
    <AppHeader />
    <div class="area-banner area-banner--console">
      <div class="container">
        <p class="eyebrow">Yönetim konsolu</p>
        <h1 class="area-banner__title">Kontrol paneli</h1>
        <nav class="subnav subnav--on-dark" aria-label="Yönetim">
          <NuxtLink to="/admin">Metrikler</NuxtLink>
          <NuxtLink to="/admin/approvals">Organizatör onayları</NuxtLink>
          <NuxtLink to="/admin/gate-staff">Kapı görevlileri</NuxtLink>
          <NuxtLink to="/admin/documents">Belgeler</NuxtLink>
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
