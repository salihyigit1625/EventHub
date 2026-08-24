<script setup lang="ts">
definePageMeta({ layout: 'auth', middleware: ['guest'] })

const auth = useAuthStore()
const notice = useNoticeStore()
const form = useZodForm(registerAttendeeSchema)
const fullName = ref('')
const email = ref('')
const password = ref('')

async function onSubmit() {
  const parsed = form.parse({
    fullName: fullName.value,
    email: email.value,
    password: password.value
  })
  if (!parsed) return
  try {
    await auth.registerAttendee(parsed)
    notice.flash(`Hesabın hazır, hoş geldin ${auth.fullName}!`)
    await navigateTo('/account')
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
        <h1>Bilet almak için kayıt ol</h1>
        <p>Cüzdan, biletler ve bekleme listesi tek hesapta.</p>
      </div>
      <div class="panel auth-card auth-card--raised">
        <h2>Kayıt ol</h2>
        <p v-if="form.formError" class="notice error">{{ form.formError }}</p>
        <form class="form-stack" style="margin-top: 1rem" @submit.prevent="onSubmit">
          <AppField label="Ad soyad" :error="form.errors.fullName">
            <input v-model="fullName" type="text" autocomplete="name">
          </AppField>
          <AppField label="E-posta" :error="form.errors.email">
            <input v-model="email" type="email" autocomplete="email">
          </AppField>
          <AppField label="Şifre" :error="form.errors.password">
            <input v-model="password" type="password" autocomplete="new-password">
          </AppField>
          <button class="btn btn-primary" type="submit" :disabled="auth.pending">
            {{ auth.pending ? 'Kaydediliyor…' : 'Kayıt ol' }}
          </button>
        </form>
        <p class="event-card__meta" style="margin-top: 1rem">
          <NuxtLink to="/login">Giriş yap</NuxtLink>
          ·
          <NuxtLink to="/register/organizer">Organizatör olmak istiyorum</NuxtLink>
        </p>
      </div>
    </div>
  </section>
</template>
