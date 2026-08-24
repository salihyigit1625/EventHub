<script setup lang="ts">
import type { PagedWaitlistDto, TicketDto, WaitlistDto } from '~/api-client'

definePageMeta({ layout: 'account', middleware: ['attendee'] })

const api = useApi()
const notice = useNoticeStore()
const action = useAsyncAction()
const { lookup } = useTicketCatalog()

const page = ref(1)

const { data, pending, refresh } = await useAsyncData(
  'my-waitlist',
  () => api.get<PagedWaitlistDto>('/api/waitlist/mine', { page: page.value, pageSize: 10 }),
  { watch: [page], lazy: true }
)

const readyCount = computed(
  () => (data.value?.items ?? []).filter(row => row.status === WaitlistStatus.Notified).length
)

async function convert(row: WaitlistDto) {
  if (!row.id) return
  await action.run(row.id, async () => {
    try {
      const ticket = await api.post<TicketDto>(`/api/waitlist/${row.id}/convert`)
      notice.flash(`Bilet oluşturuldu: ${ticket.uniqueCode}`)
      await navigateTo(`/account/tickets/${ticket.uniqueCode}`)
    }
    catch (error) {
      const apiError = toApiError(error)
      notice.fail('Bilete çevrilemedi', apiError.detail || apiError.title)
      await refresh()
    }
  })
}
</script>

<template>
  <section>
    <div class="section-head" style="margin-top: 0">
      <div>
        <h1>Bekleme listesi</h1>
        <p class="event-card__meta">
          Kontenjan dolduğunda sıraya girersin. Yer açılınca burada bilete çevirirsin.
        </p>
      </div>
      <span v-if="readyCount" class="status status--ok">{{ readyCount }} hakkın hazır</span>
    </div>

    <div v-if="pending" class="form-stack">
      <AppSkeleton variant="row" :count="3" />
    </div>

    <p v-else-if="!data?.items?.length" class="empty">
      Bekleme kaydın yok. Tükenmiş bir bilet tipinde sıraya girebilirsin.
    </p>

    <div v-else class="form-stack">
      <article
        v-for="row in data.items"
        :key="row.id"
        class="panel"
        style="display:grid; gap:0.9rem"
      >
        <header style="display:flex; justify-content:space-between; gap:1rem; flex-wrap:wrap">
          <div>
            <strong style="font-family: var(--display); font-size: 1.1rem">
              {{ lookup(row.ticketTypeId).eventTitle || `Etkinlik #${row.eventId}` }}
            </strong>
            <p class="event-card__meta" style="margin: 0">
              {{ lookup(row.ticketTypeId).typeName || `Bilet tipi #${row.ticketTypeId}` }}
            </p>
          </div>
          <NuxtLink
            v-if="row.eventId"
            class="btn btn-ghost"
            :to="`/events/${row.eventId}`"
          >
            Etkinliği gör
          </NuxtLink>
        </header>

        <WaitlistProgress :row="row" />

        <LoadingButton
          v-if="row.status === WaitlistStatus.Notified"
          class="btn-accent"
          :pending="action.isPending(row.id ?? -1)"
          pending-label="Kontenjan ayrılıyor…"
          style="justify-self: start"
          @click="convert(row)"
        >
          Hakkımı bilete çevir
        </LoadingButton>
      </article>
    </div>

    <AppPager
      :page="data?.page ?? 1"
      :page-size="data?.pageSize ?? 10"
      :total-count="data?.totalCount ?? 0"
      @change="page = $event"
    />
  </section>
</template>
