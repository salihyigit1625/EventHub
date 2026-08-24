<script setup lang="ts">
const auth = useAuthStore()
const notice = useNoticeStore()

async function onExit() {
  await auth.logout()
  notice.flash('Kapı oturumu kapatıldı.')
  await navigateTo('/login')
}
</script>

<template>
  <div class="shell shell--kiosk">
    <header class="kiosk-bar">
      <div class="kiosk-bar__title">
        <BrandLogo size="sm" />
        <span class="kiosk-bar__sep" aria-hidden="true">·</span>
        <span>Kapı</span>
        <NuxtLink class="kiosk-bar__user" to="/profile" :title="`${auth.fullName} · Profil`">
          {{ auth.fullName }}
        </NuxtLink>
      </div>
      <div style="display:flex; gap:0.45rem; align-items:center">
        <NuxtLink class="btn btn-ghost" to="/">Ana sayfa</NuxtLink>
        <button class="btn btn-ghost" type="button" @click="onExit">Çıkış</button>
      </div>
    </header>
    <slot />
  </div>
</template>
