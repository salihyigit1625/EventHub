<script setup lang="ts">
import type { TicketDto } from '~/api-client'

definePageMeta({ layout: 'account', middleware: ['attendee'] })

const route = useRoute()
const api = useApi()
const notice = useNoticeStore()
const action = useAsyncAction()
const { lookup } = useTicketCatalog()

const code = computed(() => String(route.params.code))

const { data: ticket, pending, refresh } = await useAsyncData(
  `ticket-${code.value}`,
  () => api.get<TicketDto>(`/api/tickets/by-code/${code.value}`),
  { lazy: true }
)

const context = computed(() => lookup(ticket.value?.ticketTypeId))
const canCancel = computed(
  () => ticket.value?.status === TicketStatus.Paid || ticket.value?.status === TicketStatus.Reserved
)

async function cancel() {
  if (!ticket.value?.id) return
  if (!window.confirm('Bu bilet iptal edilsin mi?')) return

  await action.run('cancel', async () => {
    try {
      await api.post<TicketDto>(`/api/tickets/${ticket.value?.id}/cancel`)
      notice.flash('Bilet iptal edildi.')
      await refresh()
    }
    catch (error) {
      const apiError = toApiError(error)
      notice.fail('İptal edilemedi', apiError.detail || apiError.title)
    }
  })
}
</script>

<template>
  <section style="max-width: 34rem; display: grid; gap: 1rem">
    <NuxtLink class="btn btn-ghost" to="/account/tickets" style="justify-self: start">
      ← Biletlerim
    </NuxtLink>

    <AppSkeleton v-if="pending" variant="ticket" />

    <template v-else-if="ticket">
      <TicketCard
        :ticket="ticket"
        :event-title="context.eventTitle"
        :type-name="context.typeName"
        :venue="context.venue"
        :start-date="context.startDate"
      />

      <div class="panel" style="display:grid; gap:0.55rem">
        <div style="display:flex; justify-content:space-between; gap:1rem">
          <span class="event-card__meta">Durum</span>
          <strong>{{ TICKET_STATUS_LABEL[ticket.status ?? 0] }}</strong>
        </div>
        <div style="display:flex; justify-content:space-between; gap:1rem">
          <span class="event-card__meta">Satın alma</span>
          <strong>{{ formatDateTime(ticket.purchasedAt) }}</strong>
        </div>
        <div style="display:flex; justify-content:space-between; gap:1rem">
          <span class="event-card__meta">Kapı girişi</span>
          <strong>{{ formatDateTime(ticket.checkedInAt) }}</strong>
        </div>
      </div>

      <NuxtLink
        v-if="context.eventId"
        class="btn btn-ghost"
        :to="`/events/${context.eventId}`"
        style="justify-self: start"
      >
        Etkinlik sayfası
      </NuxtLink>

      <LoadingButton
        v-if="canCancel"
        class="btn-danger"
        :pending="action.isPending('cancel')"
        pending-label="İptal ediliyor…"
        style="justify-self: start"
        @click="cancel"
      >
        Bileti iptal et
      </LoadingButton>
    </template>
  </section>
</template>
