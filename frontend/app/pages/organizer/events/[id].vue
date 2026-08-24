<script setup lang="ts">
import type { EventDto } from '~/api-client'
import type { TicketTypeDraft } from '~/utils/ticket-draft'

definePageMeta({ layout: 'organizer', middleware: ['organizer'] })

const route = useRoute()
const api = useApi()
const notice = useNoticeStore()
const eventForm = useZodForm(eventFormSchema)
const action = useAsyncAction()

const id = computed(() => Number(route.params.id))

const { data: event, pending, refresh } = await useAsyncData(
  () => `organizer-event-${route.params.id}`,
  () => api.get<EventDto>(`/api/events/${id.value}`),
  { watch: [id] }
)

const title = ref('')
const description = ref('')
const venue = ref('')
const startDate = ref('')
const endDate = ref('')
const cancellationDeadlineHours = ref(48)
const poster = ref<File | null>(null)
const tiers = ref<TicketTypeDraft[]>([])

watch(event, (value) => {
  if (!value) return
  title.value = value.title ?? ''
  description.value = value.description ?? ''
  venue.value = value.venue ?? ''
  startDate.value = isoToLocalInput(value.startDate)
  endDate.value = isoToLocalInput(value.endDate)
  cancellationDeadlineHours.value = value.cancellationDeadlineHours ?? 48
  tiers.value = (value.ticketTypes ?? []).map(ticketDraftFromDto)
}, { immediate: true })

const locked = computed(() => event.value?.status !== EventStatus.Draft)
const isDraft = computed(() => event.value?.status === EventStatus.Draft)
const isPublished = computed(() => event.value?.status === EventStatus.Published)
const hasPoster = computed(() => Boolean(event.value?.posterDocumentId))

const posterSrc = computed(() => {
  if (!event.value?.id || !hasPoster.value) return null
  return `${api.posterUrl(event.value.id)}?v=${event.value.posterDocumentId}`
})

/* ——— Performans ——— */
const saved = computed(() => event.value?.ticketTypes ?? [])

const totals = computed(() => {
  let capacity = 0
  let sold = 0
  let revenue = 0
  for (const type of saved.value) {
    const total = type.totalQuantity ?? 0
    const remaining = type.remainingQuantity ?? 0
    capacity += total
    sold += total - remaining
    revenue += (total - remaining) * (type.price ?? 0)
  }
  return { capacity, sold, revenue, fill: capacity ? Math.round((sold / capacity) * 100) : 0 }
})

const bars = computed(() => {
  const peak = Math.max(1, ...saved.value.map(t => (t.totalQuantity ?? 0) - (t.remainingQuantity ?? 0)))
  return saved.value.map(type => {
    const sold = (type.totalQuantity ?? 0) - (type.remainingQuantity ?? 0)
    return {
      id: type.id,
      name: type.name ?? '—',
      sold,
      revenue: sold * (type.price ?? 0),
      height: Math.round((sold / peak) * 100)
    }
  })
})

const readyToPublish = computed(() => saved.value.length > 0)

/* ——— İşlemler ——— */
async function saveEvent() {
  const parsed = eventForm.parse({
    title: title.value,
    description: description.value,
    venue: venue.value,
    startDate: startDate.value,
    endDate: endDate.value,
    cancellationDeadlineHours: cancellationDeadlineHours.value
  })
  if (!parsed) return

  await action.run('save', async () => {
    try {
      event.value = await api.put<EventDto>(`/api/events/${id.value}`, {
        title: parsed.title,
        description: parsed.description || null,
        venue: parsed.venue,
        startDate: localInputToIso(parsed.startDate),
        endDate: localInputToIso(parsed.endDate),
        cancellationDeadlineHours: parsed.cancellationDeadlineHours
      })
      notice.flash('Etkinlik güncellendi.')
      await refresh()
    }
    catch (error) {
      eventForm.fromApi(error)
      const apiError = toApiError(error)
      notice.fail('Kaydedilemedi', apiError.detail || apiError.title)
    }
  })
}

async function publish() {
  if (!readyToPublish.value) {
    notice.fail('Yayınlanamaz', 'Önce en az bir bilet tipi oluşturup kaydet.')
    return
  }
  await action.run('publish', async () => {
    try {
      event.value = await api.post<EventDto>(`/api/events/${id.value}/publish`)
      notice.flash('Etkinlik yayında. Katılımcılar bilet alabilir.')
      await refresh()
    }
    catch (error) {
      const apiError = toApiError(error)
      notice.fail('Yayınlanamadı', apiError.detail || apiError.title)
    }
  })
}

async function cancelEvent() {
  if (!window.confirm('Etkinlik iptal edilsin mi? Bu işlem geri alınamaz.')) return
  await action.run('cancel', async () => {
    try {
      event.value = await api.post<EventDto>(`/api/events/${id.value}/cancel`)
      notice.flash('Etkinlik iptal edildi.')
      await refresh()
    }
    catch (error) {
      const apiError = toApiError(error)
      notice.fail('İptal edilemedi', apiError.detail || apiError.title)
    }
  })
}

async function uploadPoster() {
  if (!poster.value) return
  await action.run('poster', async () => {
    try {
      event.value = await api.upload<EventDto>(`/api/events/${id.value}/poster`, poster.value as File)
      notice.flash('Afiş güncellendi.')
      poster.value = null
      await refresh()
    }
    catch (error) {
      const apiError = toApiError(error)
      notice.fail('Afiş yüklenemedi', apiError.detail || apiError.title)
    }
  })
}
</script>

<template>
  <div v-if="pending && !event" class="form-stack">
    <AppSkeleton variant="title" />
    <AppSkeleton variant="kpi" :count="3" />
    <AppSkeleton variant="block" />
  </div>

  <section v-else-if="event" class="form-stack" style="gap: 1.4rem">
    <div class="section-head" style="margin: 0">
      <div>
        <p class="eyebrow">Etkinlik #{{ event.id }}</p>
        <h1>{{ event.title }}</h1>
        <span
          class="status"
          :class="isPublished ? 'status--ok' : isDraft ? 'status--warn' : 'status--bad'"
        >
          {{ EVENT_STATUS_LABEL[event.status ?? 0] }}
        </span>
      </div>
      <div style="display:flex; gap:0.5rem; flex-wrap:wrap">
        <NuxtLink v-if="isPublished" class="btn btn-ghost" :to="`/events/${event.id}`">
          Genel sayfa
        </NuxtLink>
        <LoadingButton
          v-if="isDraft"
          class="btn-accent"
          :pending="action.isPending('publish')"
          :disabled="!readyToPublish"
          pending-label="Yayınlanıyor…"
          @click="publish"
        >
          Yayınla
        </LoadingButton>
        <LoadingButton
          v-if="isDraft || isPublished"
          class="btn-ghost"
          :pending="action.isPending('cancel')"
          pending-label="İptal ediliyor…"
          @click="cancelEvent"
        >
          Etkinliği iptal et
        </LoadingButton>
      </div>
    </div>

    <p v-if="isDraft && !readyToPublish" class="notice">
      Yayınlamak için en az bir bilet tipi kaydetmen gerekiyor.
    </p>

    <!-- Performans -->
    <section>
      <div class="section-head" style="margin-top: 0">
        <h2 style="font-size: 1.2rem">Performans</h2>
        <span class="pill">Anlık</span>
      </div>

      <div class="kpi-grid">
        <div class="kpi kpi--good">
          <span class="kpi__label">Satılan bilet</span>
          <span class="kpi__value">{{ totals.sold }}</span>
          <span class="kpi__hint">{{ totals.capacity }} kontenjan içinde</span>
        </div>
        <div class="kpi kpi--gold">
          <span class="kpi__label">Hasılat</span>
          <span class="kpi__value">{{ formatMoney(totals.revenue) }}</span>
          <span class="kpi__hint">Satılan biletlerin toplamı</span>
        </div>
        <div class="kpi">
          <span class="kpi__label">Doluluk</span>
          <span class="kpi__value">%{{ totals.fill }}</span>
          <span class="kpi__hint">{{ totals.capacity - totals.sold }} bilet kaldı</span>
        </div>
      </div>

      <div v-if="bars.length" class="panel" style="margin-top: 0.9rem">
        <h3 style="font-size: 1rem; margin-bottom: 0.9rem">Bilet tipine göre satış</h3>
        <div class="bars">
          <div v-for="bar in bars" :key="bar.id" class="bars__col">
            <span class="bars__value">{{ bar.sold }}</span>
            <div class="bars__bar" :style="{ height: `${Math.max(bar.height, 3)}%` }" />
            <span class="bars__label">{{ bar.name }}</span>
            <span class="bars__sub">{{ formatMoney(bar.revenue) }}</span>
          </div>
        </div>
      </div>

      <div v-if="saved.length" class="form-stack" style="margin-top: 0.9rem">
        <div v-for="type in saved" :key="type.id" class="panel" style="padding: 0.85rem 1rem">
          <div style="display:flex; justify-content:space-between; gap:1rem; margin-bottom:0.5rem">
            <strong>{{ type.name }}</strong>
            <span class="event-card__meta">{{ formatMoney(type.price) }}</span>
          </div>
          <QuotaBar
            :remaining="type.remainingQuantity"
            :total="type.totalQuantity"
            label="Kontenjan"
          />
        </div>
      </div>
    </section>

    <!-- Bilgiler -->
    <section class="panel">
      <h2 style="font-size: 1.15rem; margin-bottom: 0.8rem">Bilgiler</h2>
      <p v-if="eventForm.formError" class="notice error" style="margin-bottom: 0.8rem">
        {{ eventForm.formError }}
      </p>
      <form class="form-stack" @submit.prevent="saveEvent">
        <AppField label="Başlık" :error="eventForm.errors.title">
          <input v-model="title" type="text" :disabled="locked">
        </AppField>
        <AppField label="Mekan" :error="eventForm.errors.venue">
          <input v-model="venue" type="text" :disabled="locked">
        </AppField>
        <AppField label="Açıklama">
          <textarea v-model="description" :disabled="locked" />
        </AppField>
        <div class="form-row">
          <AppField label="Başlangıç" :error="eventForm.errors.startDate">
            <input v-model="startDate" type="datetime-local" :disabled="locked">
          </AppField>
          <AppField label="Bitiş" :error="eventForm.errors.endDate">
            <input v-model="endDate" type="datetime-local" :disabled="locked">
          </AppField>
        </div>
        <AppField label="İptal / iade süresi (saat önce)">
          <input v-model.number="cancellationDeadlineHours" type="number" min="0" :disabled="locked">
        </AppField>
        <LoadingButton
          v-if="!locked"
          class="btn-primary"
          type="submit"
          :pending="action.isPending('save')"
          pending-label="Kaydediliyor…"
          style="justify-self: start"
        >
          Kaydet
        </LoadingButton>
        <p v-else class="event-card__meta">
          Yayınlanmış etkinliğin metni kilitlidir. Bilet tipleri ve bekleme listesi yönetilebilir.
        </p>
      </form>
    </section>

    <!-- Afiş -->
    <section class="panel">
      <h2 style="font-size: 1.15rem; margin-bottom: 0.8rem">Afiş</h2>
      <PosterDropzone v-model="poster" :fallback-src="posterSrc" />
      <LoadingButton
        class="btn-soft"
        :pending="action.isPending('poster')"
        :disabled="!poster"
        pending-label="Yükleniyor…"
        style="margin-top: 0.8rem"
        @click="uploadPoster"
      >
        {{ hasPoster ? 'Afişi değiştir' : 'Afişi yükle' }}
      </LoadingButton>
    </section>

    <!-- Bilet tipleri -->
    <section class="panel">
      <TicketTypeManager
        v-model="tiers"
        :event-id="event.id"
        :suggested-start="isoToLocalInput(new Date().toISOString())"
        :suggested-end="startDate"
        @saved="refresh"
      />
    </section>
  </section>
</template>
