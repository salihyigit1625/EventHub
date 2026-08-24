<script setup lang="ts">
import type { PagedTicketDto, TicketDto } from '~/api-client'

definePageMeta({ layout: 'account', middleware: ['attendee'] })

const api = useApi()
const notice = useNoticeStore()
const action = useAsyncAction()
const { lookup } = useTicketCatalog()

const page = ref(1)
const status = ref<number | ''>('')

const FILTERS = [
  { value: '' as const, label: 'Hepsi' },
  { value: TicketStatus.Paid, label: 'Geçerli' },
  { value: TicketStatus.CheckedIn, label: 'Kullanıldı' },
  { value: TicketStatus.Cancelled, label: 'İptal' },
  { value: TicketStatus.Refunded, label: 'İade' }
]

const { data, pending, refresh } = await useAsyncData(
  'my-tickets',
  () => api.get<PagedTicketDto>('/api/tickets/mine', {
    page: page.value,
    pageSize: 10,
    status: status.value === '' ? undefined : status.value
  }),
  { watch: [page, status], lazy: true }
)

function canCancel(ticket: TicketDto) {
  return ticket.status === TicketStatus.Paid || ticket.status === TicketStatus.Reserved
}

async function cancel(ticket: TicketDto) {
  if (!ticket.id) return
  const confirmed = window.confirm(
    `${ticket.uniqueCode} numaralı bilet iptal edilsin mi? İade koşulları etkinliğin iptal süresine bağlıdır.`
  )
  if (!confirmed) return

  await action.run(ticket.id, async () => {
    try {
      await api.post<TicketDto>(`/api/tickets/${ticket.id}/cancel`)
      notice.flash('Bilet iptal edildi. Uygunsa iade cüzdanına yansır.')
      await refresh()
    }
    catch (error) {
      const apiError = toApiError(error)
      notice.fail('Bilet iptal edilemedi', apiError.detail || apiError.title)
    }
  })
}

function setFilter(value: number | '') {
  status.value = value
  page.value = 1
}
</script>

<template>
  <section>
    <div class="section-head" style="margin-top: 0">
      <div>
        <h1>Biletlerim</h1>
        <p class="event-card__meta">Karekodu kapıda okut. Bilet cüzdanından ödenir.</p>
      </div>
      <span v-if="data" class="pill">{{ data.totalCount }} bilet</span>
    </div>

    <nav class="subnav" aria-label="Bilet durumu">
      <button
        v-for="filter in FILTERS"
        :key="String(filter.value)"
        class="btn"
        :class="status === filter.value ? 'btn-accent' : 'btn-ghost'"
        type="button"
        @click="setFilter(filter.value)"
      >
        {{ filter.label }}
      </button>
    </nav>

    <AppSkeleton v-if="pending" class="ticket-wallet" :stack="false" variant="ticket" :count="3" />

    <p v-else-if="!data?.items?.length" class="empty">
      Bu filtrede bilet yok.
      <NuxtLink to="/" style="text-decoration: underline">Etkinliklere göz at</NuxtLink>.
    </p>

    <div v-else class="ticket-wallet">
      <div v-for="ticket in data.items" :key="ticket.id" style="display:grid; gap:0.55rem">
        <TicketCard
          :ticket="ticket"
          :event-title="lookup(ticket.ticketTypeId).eventTitle"
          :type-name="lookup(ticket.ticketTypeId).typeName"
          :venue="lookup(ticket.ticketTypeId).venue"
          :start-date="lookup(ticket.ticketTypeId).startDate"
          :to="`/account/tickets/${ticket.uniqueCode}`"
        />
        <div v-if="canCancel(ticket)" style="display:flex; justify-content:flex-end">
          <LoadingButton
            class="btn-ghost"
            :pending="action.isPending(ticket.id ?? -1)"
            pending-label="İptal ediliyor…"
            @click="cancel(ticket)"
          >
            Bileti iptal et
          </LoadingButton>
        </div>
      </div>
    </div>

    <AppPager
      :page="data?.page ?? 1"
      :page-size="data?.pageSize ?? 10"
      :total-count="data?.totalCount ?? 0"
      @change="page = $event"
    />
  </section>
</template>
