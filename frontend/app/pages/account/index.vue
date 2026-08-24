<script setup lang="ts">
import type { PagedTicketDto, PagedWaitlistDto, WalletBalanceDto } from '~/api-client'

definePageMeta({ layout: 'account', middleware: ['attendee'] })

const api = useApi()
const auth = useAuthStore()
const { lookup } = useTicketCatalog()

const { data: tickets, pending: ticketsPending } = await useAsyncData(
  'account-tickets',
  () => api.get<PagedTicketDto>('/api/tickets/mine', { page: 1, pageSize: 2 }),
  { lazy: true }
)
const { data: waitlist } = await useAsyncData(
  'account-waitlist',
  () => api.get<PagedWaitlistDto>('/api/waitlist/mine', { page: 1, pageSize: 5 }),
  { lazy: true }
)
const { data: wallet } = await useAsyncData(
  'account-wallet',
  () => api.get<WalletBalanceDto>('/api/wallet'),
  { lazy: true }
)

const readyCount = computed(
  () => (waitlist.value?.items ?? []).filter(row => row.status === WaitlistStatus.Notified).length
)
const lowBalance = computed(() => (wallet.value?.balance ?? 0) <= 0)
</script>

<template>
  <section class="form-stack" style="gap: 1.4rem">
    <div class="section-head" style="margin: 0">
      <div>
        <h1 style="font-size: 1.7rem">Merhaba, {{ auth.fullName }}</h1>
        <p class="event-card__meta">Biletlerin, sıran ve bakiyen.</p>
      </div>
      <NuxtLink class="btn btn-accent" to="/">Etkinliklere göz at</NuxtLink>
    </div>

    <div class="kpi-grid">
      <div class="kpi" :class="lowBalance ? 'kpi--warn' : 'kpi--gold'">
        <span class="kpi__label">Cüzdan</span>
        <span class="kpi__value">{{ formatMoney(wallet?.balance) }}</span>
        <span class="kpi__hint">Bilet bedeli buradan düşer</span>
      </div>
      <div class="kpi kpi--good">
        <span class="kpi__label">Biletlerim</span>
        <span class="kpi__value">{{ tickets?.totalCount ?? 0 }}</span>
        <span class="kpi__hint">Karekodu kapıda okut</span>
      </div>
      <div class="kpi" :class="{ 'kpi--warn': readyCount > 0 }">
        <span class="kpi__label">Bekleme sırası</span>
        <span class="kpi__value">{{ waitlist?.totalCount ?? 0 }}</span>
        <span class="kpi__hint">
          {{ readyCount > 0 ? `${readyCount} hakkın kullanılmayı bekliyor` : 'Sıradaki kayıtların' }}
        </span>
      </div>
    </div>

    <div v-if="readyCount > 0" class="notice ok">
      Bekleme listesinde sıran geldi.
      <NuxtLink to="/account/waitlist" style="text-decoration: underline">Hakkını bilete çevir</NuxtLink>
    </div>

    <div v-if="lowBalance" class="notice">
      Cüzdanında bakiye yok.
      <NuxtLink to="/account/wallet" style="text-decoration: underline">Bakiye yükle</NuxtLink>
    </div>

    <section>
      <div class="section-head" style="margin-top: 0">
        <h2 style="font-size: 1.2rem">Son biletlerin</h2>
        <NuxtLink class="btn btn-ghost" to="/account/tickets">Tümü</NuxtLink>
      </div>

      <AppSkeleton v-if="ticketsPending" class="ticket-wallet" :stack="false" variant="ticket" :count="2" />

      <p v-else-if="!tickets?.items?.length" class="empty">
        Henüz biletin yok.
        <NuxtLink to="/" style="text-decoration: underline">Etkinlik seç</NuxtLink>
      </p>

      <div v-else class="ticket-wallet">
        <TicketCard
          v-for="ticket in tickets.items"
          :key="ticket.id"
          :ticket="ticket"
          :event-title="lookup(ticket.ticketTypeId).eventTitle"
          :type-name="lookup(ticket.ticketTypeId).typeName"
          :venue="lookup(ticket.ticketTypeId).venue"
          :start-date="lookup(ticket.ticketTypeId).startDate"
          :to="`/account/tickets/${ticket.uniqueCode}`"
        />
      </div>
    </section>
  </section>
</template>
