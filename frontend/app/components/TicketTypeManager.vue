<script setup lang="ts">
import type { TicketTypeDto } from '~/api-client'
import type { TicketTypeDraft } from '~/utils/ticket-draft'

const props = withDefaults(
  defineProps<{
    /** Verilirse satırlar doğrudan API'ye yazılır, verilmezse yalnız yerelde tutulur. */
    eventId?: number
    locked?: boolean
    /** Satış tarihleri boşsa bu aralık önerilir. */
    suggestedStart?: string
    suggestedEnd?: string
    /** Üst bileşenin (sihirbaz) ürettiği satır hataları. */
    issues?: Record<string, string>
  }>(),
  {
    eventId: undefined,
    locked: false,
    suggestedStart: '',
    suggestedEnd: '',
    issues: () => ({})
  }
)

const emit = defineEmits<{ saved: [] }>()

const rows = defineModel<TicketTypeDraft[]>({ required: true })

const api = useApi()
const notice = useNoticeStore()
const action = useAsyncAction()
const errors = reactive<Record<string, string>>({})

const usedNames = computed(
  () => new Set(rows.value.map(row => row.name.toLocaleLowerCase('tr')))
)

function addRow(seed: Partial<TicketTypeDraft> = {}) {
  rows.value = [
    ...rows.value,
    blankTicketDraft({
      saleStartDate: props.suggestedStart,
      saleEndDate: props.suggestedEnd,
      ...seed
    })
  ]
}

function removeRow(key: string) {
  rows.value = rows.value.filter(row => row.key !== key)
  delete errors[key]
}

function validate(row: TicketTypeDraft) {
  const parsed = parseTicketDraft(row)
  if (!parsed.ok) {
    errors[row.key] = parsed.message
    return null
  }
  delete errors[row.key]
  return parsed.data
}

/** Satır düzenlenince üstten gelen hata mesajı geçerliliğini yitirir. */
function touch(row: TicketTypeDraft) {
  row.dirty = true
  delete errors[row.key]
}

const shownErrors = computed(() => ({ ...props.issues, ...errors }))

async function persist(row: TicketTypeDraft) {
  const parsed = validate(row)
  if (!parsed || !props.eventId) return

  await action.run(row.key, async () => {
    const body = {
      name: parsed.name,
      price: parsed.price,
      totalQuantity: parsed.totalQuantity,
      maxTicketsPerUser: parsed.maxTicketsPerUser,
      saleStartDate: localInputToIso(parsed.saleStartDate),
      saleEndDate: localInputToIso(parsed.saleEndDate)
    }
    try {
      const saved = row.id
        ? await api.put<TicketTypeDto>(`/api/ticket-types/${row.id}`, body)
        : await api.post<TicketTypeDto>('/api/ticket-types', { ...body, eventId: props.eventId })

      row.id = saved.id
      row.remainingQuantity = saved.remainingQuantity
      row.dirty = false
      notice.flash(`“${parsed.name}” kaydedildi.`)
      emit('saved')
    }
    catch (error) {
      const apiError = toApiError(error)
      errors[row.key] = apiError.detail || apiError.title
      notice.fail('Bilet tipi kaydedilemedi', apiError.detail || apiError.title)
    }
  })
}

async function notifyNext(row: TicketTypeDraft) {
  if (!row.id) return
  await action.run(`notify-${row.key}`, async () => {
    try {
      await api.post(`/api/waitlist/ticket-types/${row.id}/notify-next`)
      notice.flash('Bekleme listesindeki sıradaki kişi bilgilendirildi.')
    }
    catch (error) {
      const apiError = toApiError(error)
      notice.fail('Bildirim gönderilemedi', apiError.detail || apiError.title)
    }
  })
}

defineExpose({ addRow })
</script>

<template>
  <section class="form-stack">
    <div class="section-head" style="margin: 0">
      <div>
        <h2 style="font-size: 1.15rem">Bilet tipleri</h2>
        <p class="event-card__meta">
          Her tip kendi kontenjanı ve satış aralığıyla çalışır.
          {{ eventId ? 'Değişiklikler tek tek kaydedilir.' : 'Etkinlikle birlikte oluşturulur.' }}
        </p>
      </div>
      <button class="btn btn-primary" type="button" :disabled="locked" @click="addRow()">
        + Tip ekle
      </button>
    </div>

    <div v-if="!locked" style="display:flex; gap:0.4rem; flex-wrap:wrap">
      <button
        v-for="preset in TICKET_TYPE_PRESETS"
        :key="preset.name"
        class="btn btn-ghost"
        type="button"
        :disabled="usedNames.has(preset.name.toLocaleLowerCase('tr'))"
        @click="addRow({ ...preset })"
      >
        {{ preset.name }} şablonu
      </button>
    </div>

    <p v-if="!rows.length" class="empty">
      Henüz tip yok. Yayınlamak için en az bir bilet tipi gerekir.
    </p>

    <TransitionGroup name="fade-slide" tag="div" class="form-stack">
      <article
        v-for="row in rows"
        :key="row.key"
        class="panel form-stack"
        :class="{ 'tier--vip': isPremiumTier(row.name) }"
        style="gap: 0.75rem"
      >
        <header style="display:flex; align-items:center; gap:0.6rem; flex-wrap:wrap">
          <strong style="font-family: var(--display); font-size: 1.05rem">
            {{ row.name || 'Yeni bilet tipi' }}
          </strong>
          <span v-if="!row.id" class="status status--warn">Taslak</span>
          <span v-else-if="row.dirty" class="status status--warn">Kaydedilmedi</span>
          <span v-else class="status status--ok">Kayıtlı</span>
          <span style="flex:1" />
          <button
            v-if="!locked"
            class="btn btn-ghost"
            type="button"
            @click="removeRow(row.key)"
          >
            {{ row.id ? 'Listeden çıkar' : 'Sil' }}
          </button>
        </header>

        <QuotaBar
          v-if="row.id"
          :remaining="row.remainingQuantity ?? row.totalQuantity"
          :total="row.totalQuantity"
          label="Satış durumu"
        />

        <div class="form-row">
          <AppField label="Ad">
            <input
              v-model="row.name"
              type="text"
              maxlength="150"
              placeholder="VIP / Standart / Erken Kayıt"
              :disabled="locked"
              @input="touch(row)"
            >
          </AppField>
          <AppField label="Fiyat (₺)">
            <input
              v-model.number="row.price"
              type="number"
              min="0"
              step="0.01"
              :disabled="locked"
              @input="touch(row)"
            >
          </AppField>
        </div>

        <div class="form-row">
          <AppField label="Kontenjan">
            <input
              v-model.number="row.totalQuantity"
              type="number"
              min="1"
              :disabled="locked"
              @input="touch(row)"
            >
          </AppField>
          <AppField label="Kişi başı limit">
            <input
              v-model.number="row.maxTicketsPerUser"
              type="number"
              min="1"
              :disabled="locked"
              @input="touch(row)"
            >
          </AppField>
        </div>

        <div class="form-row">
          <AppField label="Satış başlangıcı">
            <input
              v-model="row.saleStartDate"
              type="datetime-local"
              :disabled="locked"
              @input="touch(row)"
            >
          </AppField>
          <AppField label="Satış bitişi">
            <input
              v-model="row.saleEndDate"
              type="datetime-local"
              :disabled="locked"
              @input="touch(row)"
            >
          </AppField>
        </div>

        <p v-if="shownErrors[row.key]" class="notice error">{{ shownErrors[row.key] }}</p>

        <div v-if="eventId" style="display:flex; gap:0.5rem; flex-wrap:wrap">
          <LoadingButton
            class="btn-primary"
            :pending="action.isPending(row.key)"
            :disabled="locked || !row.dirty"
            pending-label="Kaydediliyor…"
            @click="persist(row)"
          >
            {{ row.id ? 'Değişikliği kaydet' : 'Tipi oluştur' }}
          </LoadingButton>
          <LoadingButton
            v-if="row.id"
            class="btn-soft"
            :pending="action.isPending(`notify-${row.key}`)"
            pending-label="Bildiriliyor…"
            @click="notifyNext(row)"
          >
            Bekleme sırasını bildir
          </LoadingButton>
        </div>
      </article>
    </TransitionGroup>
  </section>
</template>
