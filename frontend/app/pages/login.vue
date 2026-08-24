<script setup lang="ts">
definePageMeta({ layout: 'auth', middleware: ['guest'] })

const auth = useAuthStore()
const route = useRoute()
const notice = useNoticeStore()
const form = useZodForm(loginSchema)
const email = ref('')
const password = ref('')

async function onSubmit() {
  const parsed = form.parse({ email: email.value, password: password.value })
  if (!parsed) return
  try {
    await auth.login(parsed)
    notice.flash(`Hoş geldin, ${auth.fullName || 'EventHub'}!`)
    const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : homePathForRoles(auth.roles)
    await navigateTo(redirect || '/')
  }
  catch (error) {
    form.fromApi(error)
  }
}
</script>

<template>
  <section class="auth-shell">
    <div class="auth-stage">
      <div class="auth-stage__aside">
        <BrandLogo size="lg" :link="false" />
        <h1>Tekrar merhaba</h1>
        <p>Giriş yapınca bilet alabilir, cüzdanını yönetebilirsin.</p>
      </div>
      <div class="panel auth-card auth-card--raised">
        <p class="eyebrow">Hesap</p>
        <h2>Giriş yap</h2>
        <p v-if="form.formError" class="notice error">{{ form.formError }}</p>
        <form class="form-stack" style="margin-top: 1rem" @submit.prevent="onSubmit">
          <AppField label="E-posta" :error="form.errors.email">
            <input v-model="email" type="email" autocomplete="email">
          </AppField>
          <AppField label="Şifre" :error="form.errors.password">
            <input v-model="password" type="password" autocomplete="current-password">
          </AppField>
          <button class="btn btn-primary" type="submit" :disabled="auth.pending">
            {{ auth.pending ? 'Giriş yapılıyor…' : 'Giriş yap' }}
          </button>
        </form>
        <p class="event-card__meta" style="margin-top: 1rem">
          Hesabın yok mu?
          <NuxtLink to="/register">Katılımcı kaydı</NuxtLink>
          ·
          <NuxtLink to="/register/organizer">Organizatör kaydı</NuxtLink>
        </p>
      </div>
    </div>
  </section>
</template>
