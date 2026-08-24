<script setup lang="ts">
import type { GlobalStatsDto, OrganizerProfileDto, PagedOrganizerProfileDto } from '~/api-client'

definePageMeta({ layout: 'admin', middleware: ['admin'] })

const api = useApi()
const { occupancy, index, pending: catalogPending } = useTicketCatalog()

const { data: stats, pending } = await useAsyncData(
  'admin-stats',
  () => api.get<GlobalStatsDto>('/api/admin/stats'),
  { lazy: true }
)

const { data: queue } = await useAsyncData(
  'admin-pending-preview',
  () => api.get<PagedOrganizerProfileDto>('/api/admin/organizers/pending', { page: 1, pageSize: 5 }),
  { lazy: true }
)

const pendingRows = computed<OrganizerProfileDto[]>(() => queue.value?.items ?? [])
</script>

<template>
  <section class="form-stack" style="gap: 1.4rem">
    <div class="section-head" style="margin: 0">
      <div>
        <h1 style="font-size: 1.6rem">Metrikler</h1>
        <p class="event-card__meta">Platformun anlık durumu ve bekleyen işler.</p>
      </div>
      <span class="pill">Canlı</span>
    </div>

    <AppSkeleton
      v-if="pending && !stats"
      class="kpi-grid"
      :stack="false"
      variant="kpi"
      :count="4"
    />

    <div v-else class="kpi-grid">
      <div class="kpi kpi--gold">
        <span class="kpi__label">Toplam hasılat</span>
        <span class="kpi__value">{{ formatMoney(stats?.totalRevenue) }}</span>
        <span class="kpi__hint">{{ stats?.ticketsSold ?? 0 }} bilet satıldı</span>
      </div>
      <div class="kpi kpi--good">
        <span class="kpi__label">Aktif etkinlik</span>
        <span class="kpi__value">{{ stats?.publishedEvents ?? 0 }}</span>
        <span class="kpi__hint">{{ stats?.totalEvents ?? 0 }} etkinlik kayıtlı</span>
      </div>
      <div class="kpi" :class="{ 'kpi--warn': (stats?.pendingOrganizers ?? 0) > 0 }">
        <span class="kpi__label">Bekleyen onay</span>
        <span class="kpi__value">{{ stats?.pendingOrganizers ?? 0 }}</span>
        <span class="kpi__hint">Organizatör başvurusu</span>
      </div>
      <div class="kpi">
        <span class="kpi__label">Doluluk oranı</span>
        <span class="kpi__value">
          <template v-if="catalogPending && !index?.capacity">—</template>
          <template v-else>%{{ occupancy }}</template>
        </span>
        <span class="kpi__hint">Yayındaki {{ index?.events ?? 0 }} etkinlik üzerinden</span>
      </div>
    </div>

    <section class="panel">
      <div class="section-head" style="margin-top: 0">
        <h2 style="font-size: 1.15rem">Onay kuyruğu</h2>
        <NuxtLink class="btn btn-ghost" to="/admin/approvals">Tümünü aç</NuxtLink>
      </div>

      <p v-if="!pendingRows.length" class="empty" style="padding: 1rem 0">
        Bekleyen organizatör başvurusu yok.
      </p>

      <table v-else class="table table--compact">
        <thead>
          <tr>
            <th>Şirket</th>
            <th>Başvuran</th>
            <th>Durum</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="row in pendingRows" :key="row.userId">
            <td><strong>{{ row.companyName }}</strong></td>
            <td>
              {{ row.fullName }}
              <div class="event-card__meta">{{ row.email }}</div>
            </td>
            <td><span class="status status--warn">Bekliyor</span></td>
          </tr>
        </tbody>
      </table>
    </section>

    <div style="display:flex; gap:0.5rem; flex-wrap:wrap">
      <NuxtLink class="btn btn-primary" to="/admin/approvals">Onayları yönet</NuxtLink>
      <NuxtLink class="btn btn-soft" to="/admin/gate-staff">Kapı görevlisi ata</NuxtLink>
      <NuxtLink class="btn btn-ghost" to="/admin/documents">Belgeler</NuxtLink>
    </div>
  </section>
</template>
