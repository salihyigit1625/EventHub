<script setup lang="ts">
import type { EventDto, TicketTypeDto } from '~/api-client'
import type { TicketTypeDraft } from '~/utils/ticket-draft'

definePageMeta({ layout: 'organizer', middleware: ['organizer'] })

const api = useApi()
const notice = useNoticeStore()
const form = useZodForm(eventFormSchema)
const action = useAsyncAction()

const STEPS = ['Temel bilgiler', 'Afiş', 'Bilet tipleri', 'Politika ve yayın'] as const

const step = ref(0)
const maxReached = ref(0)

const title = ref('')
const description = ref('')
const venue = ref('')
const startDate = ref('')
const endDate = ref('')
const cancellationDeadlineHours = ref(48)
const poster = ref<File | null>(null)
const tiers = ref<TicketTypeDraft[]>([])
const tierIssues = ref<Record<string, string>>({})

const stepError = ref('')

/** Satırlar değişince eski uyarılar geçerliliğini yitirir. */
watch(tiers, () => {
  if (Object.keys(tierIssues.value).length) tierIssues.value = {}
}, { deep: true })

const basicsValid = computed(() =>
  Boolean(title.value.trim() && venue.value.trim() && startDate.value && endDate.value)
  && new Date(endDate.value) > new Date(startDate.value)
)

function validateStep(index: number) {
  stepError.value = ''
  if (index === 0) {
    if (!basicsValid.value) {
      stepError.value = 'Başlık, mekan ve tarihler zorunlu. Bitiş, başlangıçtan sonra olmalı.'
      return false
    }
  }
  if (index === 2) {
    if (!tiers.value.length) {
      stepError.value = 'En az bir bilet tipi ekle.'
      return false
    }
    const issues = validateTicketDrafts(tiers.value)
    tierIssues.value = issues
    if (Object.keys(issues).length) {
      stepError.value = describeTicketIssues(tiers.value, issues)
      return false
    }
  }
  return true
}

function goTo(index: number) {
  if (index <= maxReached.value) {
    stepError.value = ''
    step.value = index
  }
}

function next() {
  if (!validateStep(step.value)) return
  step.value = Math.min(step.value + 1, STEPS.length - 1)
  maxReached.value = Math.max(maxReached.value, step.value)
}

function back() {
  stepError.value = ''
  step.value = Math.max(step.value - 1, 0)
}

async function publishDraft() {
  if (!validateStep(0) || !validateStep(2)) {
    step.value = basicsValid.value ? 2 : 0
    return
  }

  const parsed = form.parse({
    title: title.value,
    description: description.value,
    venue: venue.value,
    startDate: startDate.value,
    endDate: endDate.value,
    cancellationDeadlineHours: cancellationDeadlineHours.value
  })
  if (!parsed) return

  await action.run('create', async () => {
    let created: EventDto
    try {
      created = await api.post<EventDto>('/api/events', {
        title: parsed.title,
        description: parsed.description || null,
        venue: parsed.venue,
        startDate: localInputToIso(parsed.startDate),
        endDate: localInputToIso(parsed.endDate),
        cancellationDeadlineHours: parsed.cancellationDeadlineHours
      })
    }
    catch (error) {
      form.fromApi(error)
      const apiError = toApiError(error)
      notice.fail('Etkinlik oluşturulamadı', apiError.detail || apiError.title)
      step.value = 0
      return
    }

    if (poster.value && created.id) {
      try {
        await api.upload<EventDto>(`/api/events/${created.id}/poster`, poster.value)
      }
      catch (error) {
        notice.fail('Afiş yüklenemedi', toApiError(error).detail || 'Etkinlik sayfasından tekrar deneyebilirsin.')
      }
    }

    const failed: string[] = []
    for (const tier of tiers.value) {
      try {
        await api.post<TicketTypeDto>('/api/ticket-types', {
          eventId: created.id,
          name: tier.name,
          price: tier.price,
          totalQuantity: tier.totalQuantity,
          maxTicketsPerUser: tier.maxTicketsPerUser,
          saleStartDate: localInputToIso(tier.saleStartDate),
          saleEndDate: localInputToIso(tier.saleEndDate)
        })
      }
      catch (error) {
        const apiError = toApiError(error)
        failed.push(`${tier.name} (${apiError.detail || apiError.title})`)
      }
    }

    if (failed.length)
      notice.fail('Bazı bilet tipleri eklenemedi', `${failed.join(' · ')} — etkinlik sayfasından tekrar dene.`)
    else
      notice.flash('Etkinlik taslağı hazır. Kontrol edip yayınlayabilirsin.')

    await navigateTo(`/organizer/events/${created.id}`)
  })
}

/** Satış aralığı önerisi: bugünden etkinlik başlangıcına kadar. */
const suggestedStart = computed(() => isoToLocalInput(new Date().toISOString()))
const suggestedEnd = computed(() => startDate.value)
</script>

<template>
  <section style="max-width: 46rem">
    <div class="section-head" style="margin-top: 0">
      <div>
        <h1>Yeni etkinlik</h1>
        <p class="event-card__meta">Dört adımda taslağını hazırla; yayın son adımda.</p>
      </div>
    </div>

    <AppStepper :steps="STEPS" :current="step" :max-reached="maxReached" @go="goTo" />

    <p v-if="form.formError" class="notice error" style="margin-bottom: 1rem">{{ form.formError }}</p>
    <p v-if="stepError" class="notice error" style="margin-bottom: 1rem">{{ stepError }}</p>

    <div class="panel">
      <Transition name="fade-slide" mode="out-in">
        <!-- 1. Temel bilgiler -->
        <div v-if="step === 0" key="basics" class="form-stack">
          <h2 style="font-size: 1.15rem">Temel bilgiler</h2>
          <AppField label="Başlık" :error="form.errors.title">
            <input v-model="title" type="text" maxlength="200" placeholder="Örn. Yaz Sahnesi 2026">
          </AppField>
          <AppField label="Mekan" :error="form.errors.venue">
            <input v-model="venue" type="text" maxlength="300" placeholder="Örn. Harbiye Açıkhava">
          </AppField>
          <AppField label="Açıklama" :error="form.errors.description">
            <textarea v-model="description" maxlength="4000" placeholder="Sahne düzeni, sanatçılar, kapı saati…" />
          </AppField>
          <div class="form-row">
            <AppField label="Başlangıç" :error="form.errors.startDate">
              <input v-model="startDate" type="datetime-local">
            </AppField>
            <AppField label="Bitiş" :error="form.errors.endDate">
              <input v-model="endDate" type="datetime-local">
            </AppField>
          </div>
        </div>

        <!-- 2. Afiş -->
        <div v-else-if="step === 1" key="poster" class="form-stack">
          <h2 style="font-size: 1.15rem">Afiş</h2>
          <p class="event-card__meta">
            Kart ve etkinlik sayfasında bu görsel kullanılır. Şimdi atlayıp sonra da yükleyebilirsin.
          </p>
          <PosterDropzone v-model="poster" />
        </div>

        <!-- 3. Bilet tipleri -->
        <div v-else-if="step === 2" key="tiers">
          <TicketTypeManager
            v-model="tiers"
            :issues="tierIssues"
            :suggested-start="suggestedStart"
            :suggested-end="suggestedEnd"
          />
        </div>

        <!-- 4. Politika ve özet -->
        <div v-else key="policy" class="form-stack">
          <h2 style="font-size: 1.15rem">İptal / iade politikası</h2>
          <AppField
            label="Etkinlikten kaç saat öncesine kadar iade edilebilir?"
            :error="form.errors.cancellationDeadlineHours"
          >
            <input v-model.number="cancellationDeadlineHours" type="number" min="0" max="8760">
          </AppField>
          <p class="event-card__meta">
            Katılımcı, etkinlik başlangıcından {{ cancellationDeadlineHours }} saat öncesine kadar
            iptal ederse tutar cüzdanına iade edilir.
          </p>

          <div class="panel" style="background: rgba(55, 82, 87, 0.06)">
            <h3 style="font-size: 1rem; margin-bottom: 0.5rem">Özet</h3>
            <div class="form-stack" style="gap: 0.35rem">
              <div style="display:flex; justify-content:space-between; gap:1rem">
                <span class="event-card__meta">Etkinlik</span><strong>{{ title || '—' }}</strong>
              </div>
              <div style="display:flex; justify-content:space-between; gap:1rem">
                <span class="event-card__meta">Mekan</span><strong>{{ venue || '—' }}</strong>
              </div>
              <div style="display:flex; justify-content:space-between; gap:1rem">
                <span class="event-card__meta">Tarih</span>
                <strong>{{ startDate ? formatDateTime(localInputToIso(startDate)) : '—' }}</strong>
              </div>
              <div style="display:flex; justify-content:space-between; gap:1rem">
                <span class="event-card__meta">Afiş</span><strong>{{ poster ? poster.name : 'Yok' }}</strong>
              </div>
              <div style="display:flex; justify-content:space-between; gap:1rem">
                <span class="event-card__meta">Bilet tipi</span><strong>{{ tiers.length }} adet</strong>
              </div>
            </div>
          </div>
        </div>
      </Transition>

      <div class="wizard-actions">
        <button class="btn btn-ghost" type="button" :disabled="step === 0" @click="back">
          ← Geri
        </button>
        <button v-if="step < STEPS.length - 1" class="btn btn-primary" type="button" @click="next">
          Devam →
        </button>
        <LoadingButton
          v-else
          class="btn-accent"
          :pending="action.isPending('create')"
          pending-label="Oluşturuluyor…"
          @click="publishDraft"
        >
          Taslağı oluştur
        </LoadingButton>
      </div>
    </div>
  </section>
</template>
