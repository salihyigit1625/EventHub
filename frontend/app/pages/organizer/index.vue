<script setup lang="ts">
import type { EventListItemDto, PagedEventListItemDto } from '~/api-client'

definePageMeta({ layout: 'organizer', middleware: ['organizer'] })

const api = useApi()
const page = ref(1)
const status = ref<number | ''>('')

const FILTERS = [
  { value: '' as const, label: 'Hepsi' },
  { value: EventStatus.Draft, label: 'Taslak' },
  { value: EventStatus.Published, label: 'Yayında' },
  { value: EventStatus.Completed, label: 'Tamamlandı' },
  { value: EventStatus.Cancelled, label: 'İptal' }
]

const { data, pending } = await useAsyncData(
  'organizer-events',
  () => api.get<PagedEventListItemDto>('/api/events/mine', {
    page: page.value,
    pageSize: 10,
    status: status.value === '' ? undefined : status.value
  }),
  { watch: [page, status], lazy: true }
)

function statusTone(event: EventListItemDto) {
  if (event.status === EventStatus.Published) return 'status--ok'
  if (event.status === EventStatus.Cancelled) return 'status--bad'
  return 'status--warn'
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
        <h1 style="font-size: 1.6rem">Etkinliklerim</h1>
        <p class="event-card__meta">Taslak oluştur, afiş ve bilet tipi ekle, sonra yayınla.</p>
      </div>
      <NuxtLink class="btn btn-primary" to="/organizer/events/new">+ Yeni etkinlik</NuxtLink>
    </div>

    <nav class="subnav" aria-label="Etkinlik durumu">
      <button
        v-for="filter in FILTERS"
        :key="String(filter.value)"
        class="btn"
        :class="status === filter.value ? 'btn-primary' : 'btn-ghost'"
        type="button"
        @click="setFilter(filter.value)"
      >
        {{ filter.label }}
      </button>
    </nav>

    <AppSkeleton v-if="pending && !data" variant="row" :count="4" />

    <p v-else-if="!data?.items?.length" class="empty">
      Bu filtrede etkinlik yok. Yeni bir taslakla başlayabilirsin.
    </p>

    <table v-else class="table table--compact">
      <thead>
        <tr>
          <th>Etkinlik</th>
          <th>Tarih</th>
          <th>Durum</th>
          <th />
        </tr>
      </thead>
      <tbody>
        <tr v-for="event in data.items" :key="event.id">
          <td>
            <strong>{{ event.title }}</strong>
            <div class="event-card__meta">{{ event.venue }}</div>
          </td>
          <td>{{ formatDateTime(event.startDate) }}</td>
          <td>
            <span class="status" :class="statusTone(event)">
              {{ EVENT_STATUS_LABEL[event.status ?? 0] }}
            </span>
          </td>
          <td>
            <div style="display:flex; gap:0.4rem; justify-content:flex-end; flex-wrap:wrap">
              <NuxtLink
                v-if="event.status === EventStatus.Published"
                class="btn btn-ghost"
                :to="`/events/${event.id}`"
              >
                Önizle
              </NuxtLink>
              <NuxtLink class="btn btn-primary" :to="`/organizer/events/${event.id}`">
                Yönet
              </NuxtLink>
            </div>
          </td>
        </tr>
      </tbody>
    </table>

    <AppPager
      :page="data?.page ?? 1"
      :page-size="data?.pageSize ?? 10"
      :total-count="data?.totalCount ?? 0"
      @change="page = $event"
    />
  </section>
</template>
