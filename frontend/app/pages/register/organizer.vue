<script setup lang="ts">
definePageMeta({ layout: 'auth', middleware: ['guest'] })

const auth = useAuthStore()
const notice = useNoticeStore()
const form = useZodForm(registerOrganizerSchema)
const fullName = ref('')
const email = ref('')
const password = ref('')
const companyName = ref('')
const taxNumber = ref('')

async function onSubmit() {
  const parsed = form.parse({
    fullName: fullName.value,
    email: email.value,
    password: password.value,
    companyName: companyName.value,
    taxNumber: taxNumber.value
  })
  if (!parsed) return
  try {
    await auth.registerOrganizer({
      ...parsed,
      taxNumber: parsed.taxNumber || undefined
    })
    notice.flash('Başvurun alındı. Onay sonrası etkinlik açabilirsin.')
    await navigateTo('/organizer')
  }
  catch (error) {
    form.fromApi(error)
  }
}
</script>

<template>
  <section class="auth-shell">
    <div class="auth-stage">
      <div class="auth-stage__aside auth-stage__aside--clay">
        <BrandLogo size="lg" :link="false" />
        <h1>Sahneni EventHub’a taşı</h1>
        <p>Başvurun yönetici onayına düşer. Onaydan sonra etkinlik yayınlayabilirsin.</p>
      </div>
      <div class="panel auth-card auth-card--raised">
        <h2>Organizatör kaydı</h2>
        <p v-if="form.formError" class="notice error">{{ form.formError }}</p>
        <form class="form-stack" style="margin-top: 1rem" @submit.prevent="onSubmit">
          <AppField label="Ad soyad" :error="form.errors.fullName">
            <input v-model="fullName" type="text">
          </AppField>
          <AppField label="Şirket" :error="form.errors.companyName">
            <input v-model="companyName" type="text">
          </AppField>
          <AppField label="Vergi no" :error="form.errors.taxNumber">
            <input v-model="taxNumber" type="text">
          </AppField>
          <AppField label="E-posta" :error="form.errors.email">
            <input v-model="email" type="email">
          </AppField>
          <AppField label="Şifre" :error="form.errors.password">
            <input v-model="password" type="password" autocomplete="new-password">
          </AppField>
          <button class="btn btn-accent" type="submit" :disabled="auth.pending">
            {{ auth.pending ? 'Gönderiliyor…' : 'Başvur' }}
          </button>
        </form>
      </div>
    </div>
  </section>
</template>
