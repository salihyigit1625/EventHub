<script setup lang="ts">
const auth = useAuthStore()
const notice = useNoticeStore()
const route = useRoute()

const panelPath = computed(() => homePathForRoles(auth.roles))
const panelLabel = computed(() => {
  if (auth.isAttendee) return 'Hesabım'
  if (auth.isOrganizer) return 'Etkinliklerim'
  if (auth.isGateStaff) return 'Check-in'
  if (auth.isAdmin) return 'Yönetim'
  return 'Panelim'
})

const initials = computed(() =>
  (auth.fullName || '?')
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map(part => part.charAt(0).toLocaleUpperCase('tr'))
    .join('')
)

/** Sayfa kaydırıldığında başlık zeminini opaklaştırıp gölge veriyoruz. */
const lifted = ref(false)

onMounted(() => {
  const onScroll = () => {
    lifted.value = window.scrollY > 8
  }
  onScroll()
  window.addEventListener('scroll', onScroll, { passive: true })
  onBeforeUnmount(() => window.removeEventListener('scroll', onScroll))
})

async function onLogout() {
  const name = auth.fullName
  await auth.logout()
  notice.flash(name ? `${name}, çıkış yaptın.` : 'Çıkış yaptın.')
  await navigateTo('/')
}
</script>

<template>
  <header
    class="site-header"
    :class="{ 'site-header--authed': auth.isAuthenticated, 'site-header--lifted': lifted }"
  >
    <div class="container site-header__inner">
      <BrandLogo class="site-header__brand" size="md" tagline />

      <nav class="nav-actions" aria-label="Ana menü">
        <NuxtLink class="nav-link" to="/">Etkinlikler</NuxtLink>

        <Transition name="chip" mode="out-in">
          <div v-if="auth.isAuthenticated" key="in" class="nav-authed">
            <NuxtLink class="nav-link" :to="panelPath">
              {{ panelLabel }}
            </NuxtLink>
            <NuxtLink
              class="user-chip"
              to="/profile"
              :title="`${auth.fullName} · Profil`"
            >
              <span class="user-chip__avatar" aria-hidden="true">{{ initials }}</span>
              <span class="user-chip__name">{{ auth.fullName }}</span>
            </NuxtLink>
            <button class="btn btn-outline" type="button" @click="onLogout">
              Çıkış
            </button>
          </div>
          <div v-else key="out" class="nav-guest">
            <NuxtLink
              class="btn btn-outline"
              :to="loginWithRedirect(route.fullPath === '/login' ? '/' : route.fullPath)"
            >
              Giriş yap
            </NuxtLink>
            <NuxtLink class="btn btn-cta" to="/register">
              Ücretsiz kayıt ol
            </NuxtLink>
          </div>
        </Transition>
      </nav>
    </div>
  </header>
</template>
