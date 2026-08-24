<script setup lang="ts">
import type { GateStaffProfileDto } from '~/api-client'

definePageMeta({ layout: 'admin', middleware: ['admin'] })

const api = useApi()
const notice = useNoticeStore()
const createForm = useZodForm(createGateStaffSchema)
const assignForm = useZodForm(assignGateStaffSchema)

const email = ref('')
const password = ref('')
const fullName = ref('')
const assignedEventId = ref('')
const gateStaffUserId = ref<number | null>(null)
const eventId = ref<number | null>(null)
const created = ref<GateStaffProfileDto | null>(null)

async function createStaff() {
  const parsed = createForm.parse({
    email: email.value,
    password: password.value,
    fullName: fullName.value,
    assignedEventId: assignedEventId.value
  })
  if (!parsed) return
  try {
    created.value = await api.post<GateStaffProfileDto>('/api/admin/gate-staff', {
      email: parsed.email,
      password: parsed.password,
      fullName: parsed.fullName,
      assignedEventId: parsed.assignedEventId ? Number(parsed.assignedEventId) : null
    })
    notice.flash(`Kapı görevlisi oluşturuldu (#${created.value.userId})`)
  }
  catch (error) {
    createForm.fromApi(error)
  }
}

async function assign() {
  const parsed = assignForm.parse({
    gateStaffUserId: gateStaffUserId.value,
    eventId: eventId.value
  })
  if (!parsed) return
  try {
    created.value = await api.post<GateStaffProfileDto>('/api/admin/gate-staff/assign', parsed)
    notice.flash('Etkinlik ataması yapıldı.')
  }
  catch (error) {
    assignForm.fromApi(error)
  }
}
</script>

<template>
  <section>
    <h1>Kapı görevlileri</h1>
    <div class="detail-layout" style="margin-top: 1rem">
      <form class="panel form-stack" @submit.prevent="createStaff">
        <h2>Yeni görevli</h2>
        <p v-if="createForm.formError" class="notice error">{{ createForm.formError }}</p>
        <AppField label="Ad soyad" :error="createForm.errors.fullName">
          <input v-model="fullName" type="text">
        </AppField>
        <AppField label="E-posta" :error="createForm.errors.email">
          <input v-model="email" type="email">
        </AppField>
        <AppField label="Şifre" :error="createForm.errors.password">
          <input v-model="password" type="password">
        </AppField>
        <AppField label="Etkinlik id (opsiyonel)">
          <input v-model="assignedEventId" type="number" min="1">
        </AppField>
        <button class="btn btn-primary" type="submit">Oluştur</button>
      </form>

      <form class="panel form-stack" @submit.prevent="assign">
        <h2>Etkinliğe ata</h2>
        <p v-if="assignForm.formError" class="notice error">{{ assignForm.formError }}</p>
        <AppField label="Görevli kullanıcı id" :error="assignForm.errors.gateStaffUserId">
          <input v-model.number="gateStaffUserId" type="number" min="1">
        </AppField>
        <AppField label="Etkinlik id" :error="assignForm.errors.eventId">
          <input v-model.number="eventId" type="number" min="1">
        </AppField>
        <button class="btn btn-soft" type="submit">Ata</button>
        <div v-if="created" class="notice ok">
          #{{ created.userId }} · {{ created.fullName }} ·
          {{ created.assignedEventTitle || created.assignedEventId || 'atanmamış' }}
        </div>
      </form>
    </div>
  </section>
</template>
