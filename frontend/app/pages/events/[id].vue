<script setup lang="ts">
import type { EventDto, TicketDto, TicketTypeDto, WaitlistDto } from '~/api-client'

definePageMeta({ layout: 'event' })

const route = useRoute()
const auth = useAuthStore()
const notice = useNoticeStore()
const api = useApi()
const action = useAsyncAction()

const id = computed(() => Number(route.params.id))

const { data: event, pending, error, refresh } = await useAsyncData(
  () => `event-${route.params.id}`,
  () => api.get<EventDto>(`/api/events/${id.value}`),
  { watch: [id] }
)

/** Satın alma yalnız katılımcı; misafir önce girişe yönlenir. */
const showPurchaseActions = computed(() => !auth.isAuthenticated || auth.isAttendee)
const isStaffView = computed(
  () => auth.isAuthenticated && (auth.isOrganizer || auth.isAdmin || auth.isGateStaff) && !auth.isAttendee
)
const ownsEvent = computed(
  () => auth.isOrganizer && event.value?.organizerId != null && event.value.organizerId === auth.user?.userId
)

const posterSrc = computed(() => {
  if (!event.value?.id || !event.value.posterDocumentId) return ''
  return `${api.posterUrl(event.value.id)}?v=${event.value.posterDocumentId}`
})

const tiers = computed(() =>
  [...(event.value?.ticketTypes ?? [])].sort((a, b) => (a.price ?? 0) - (b.price ?? 0))
)

const cheapest = computed(() =>
  tiers.value.length ? Math.min(...tiers.value.map(type => type.price ?? 0)) : null
)

const totalRemaining = computed(() =>
  tiers.value.reduce((sum, type) => sum + (type.remainingQuantity ?? 0), 0)
)

async function buy(type: TicketTypeDto) {
  if (!auth.isAuthenticated) {
    await navigateTo(loginWithRedirect(`/events/${id.value}`))
    return
  }
  if (!auth.isAttendee) {
    notice.fail('Bu hesapla bilet alınamaz', 'Bilet satın alma yalnızca katılımcı hesaplarına açıktır.')
    return
  }

  await action.run(type.id ?? -1, async () => {
    try {
      const ticket = await api.post<TicketDto>(`/api/tickets/purchase/${type.id}`)
      notice.flash(`Bilet alındı · ${ticket.uniqueCode}`)
      await navigateTo(`/account/tickets/${ticket.uniqueCode}`)
    }
    catch (err) {
      const apiError = toApiError(err)
      notice.fail('Bilet alınamadı', apiError.detail || apiError.title)
      await refresh()
    }
  })
}

async function joinWaitlist(type: TicketTypeDto) {
  if (!auth.isAuthenticated) {
    await navigateTo(loginWithRedirect(`/events/${id.value}`))
    return
  }
  if (!auth.isAttendee) {
    notice.fail('Bu hesapla sıraya girilemez', 'Bekleme listesi yalnızca katılımcı hesapları içindir.')
    return
  }

  await action.run(`wl-${type.id}`, async () => {
    try {
      await api.post<WaitlistDto>(`/api/waitlist/join/${type.id}`)
      notice.flash('Bekleme listesine eklendin. Yer açılınca haber vereceğiz.')
      await navigateTo('/account/waitlist')
    }
    catch (err) {
      const apiError = toApiError(err)
      notice.fail('Sıraya girilemedi', apiError.detail || apiError.title)
    }
  })
}
</script>

<template>
  <div v-if="pending && !event" style="display:grid; gap:1.2rem">
    <AppSkeleton variant="media" />
    <AppSkeleton variant="title" />
    <AppSkeleton variant="text" :count="3" />
  </div>

  <p v-else-if="error" class="notice error">
    Etkinlik yüklenemedi. {{ error.message }}
  </p>

  <article v-else-if="event">
    <!-- Sahne: afiş + geri sayım -->
    <section class="event-stage">
      <div class="event-stage__art">
        <img v-if="posterSrc" :src="posterSrc" :alt="`${event.title} afişi`">
        <div v-else class="poster-empty">Afiş yok</div>
      </div>

      <div class="event-stage__intro">
        <p class="eyebrow">{{ EVENT_STATUS_LABEL[event.status ?? 0] }}</p>
        <h1>{{ event.title }}</h1>
        <p class="event-card__meta">
          {{ event.venue }} · {{ formatDateTime(event.startDate) }}
        </p>

        <div style="margin: 1.1rem 0">
          <p class="eyebrow" style="margin-bottom: 0.45rem">Başlamasına</p>
          <CountdownTimer :to="event.startDate" />
        </div>

        <div class="event-stage__facts">
          <div>
            <span>Başlangıç fiyatı</span>
            <strong>{{ cheapest === null ? '—' : formatMoney(cheapest) }}</strong>
          </div>
          <div>
            <span>Kalan bilet</span>
            <strong>{{ totalRemaining }}</strong>
          </div>
          <div>
            <span>İade süresi</span>
            <strong>{{ event.cancellationDeadlineHours }} saat önce</strong>
          </div>
        </div>
      </div>
    </section>

    <!-- Oturum durumu -->
    <div
      class="detail-session"
      :class="{
        'detail-session--guest': !auth.isAuthenticated,
        'detail-session--attendee': auth.isAttendee,
        'detail-session--staff': isStaffView
      }"
    >
      <template v-if="!auth.isAuthenticated">
        <strong>Misafir</strong>
        <span>Serbestçe inceleyebilirsin. Bilet almak için giriş gerekir.</span>
        <NuxtLink class="btn btn-accent" :to="loginWithRedirect(`/events/${id}`)">Giriş yap</NuxtLink>
      </template>
      <template v-else-if="auth.isAttendee">
        <strong>{{ auth.fullName }}</strong>
        <span>Bilet bedeli cüzdanından düşülür.</span>
        <NuxtLink class="btn btn-ghost" to="/account/wallet">Cüzdanım</NuxtLink>
      </template>
      <template v-else>
        <strong>{{ auth.isOrganizer ? 'Organizatör' : auth.isAdmin ? 'Yönetici' : 'Kapı görevlisi' }} görünümü</strong>
        <span>Bu sayfada satış yok — yalnızca katalog.</span>
        <NuxtLink v-if="ownsEvent" class="btn btn-primary" :to="`/organizer/events/${event.id}`">
          Etkinliği yönet
        </NuxtLink>
        <NuxtLink v-else class="btn btn-ghost" :to="homePathForRoles(auth.roles)">Panele dön</NuxtLink>
      </template>
    </div>

    <!-- Bilet tipleri: yatay karşılaştırma -->
    <div class="section-head">
      <h2>{{ auth.isAttendee ? 'Biletini seç' : 'Bilet tipleri' }}</h2>
      <span v-if="tiers.length" class="pill">{{ tiers.length }} seçenek</span>
    </div>

    <p v-if="!tiers.length" class="empty">Bu etkinlik için henüz bilet tipi tanımlanmamış.</p>

    <div v-else class="tier-grid">
      <article
        v-for="type in tiers"
        :key="type.id"
        class="tier"
        :class="{
          'tier--vip': isPremiumTier(type.name),
          'tier--sold': (type.remainingQuantity ?? 0) === 0
        }"
      >
        <span v-if="isPremiumTier(type.name)" class="tier__ribbon">Ayrıcalıklı</span>

        <h3 class="tier__name">{{ type.name }}</h3>
        <p class="tier__price">{{ formatMoney(type.price) }}</p>

        <div class="tier__meta">
          <span>Kişi başı en fazla {{ type.maxTicketsPerUser }} bilet</span>
          <span>Satış: {{ formatDateTime(type.saleStartDate) }} — {{ formatDateTime(type.saleEndDate) }}</span>
        </div>

        <QuotaBar
          :remaining="type.remainingQuantity"
          :total="type.totalQuantity"
          label="Kontenjan"
        />

        <div class="tier__foot">
          <template v-if="showPurchaseActions">
            <LoadingButton
              v-if="(type.remainingQuantity ?? 0) > 0"
              class="btn-accent"
              :pending="action.isPending(type.id ?? -1)"
              pending-label="Kontenjan ayrılıyor…"
              @click="buy(type)"
            >
              {{ auth.isAuthenticated ? 'Bilet al' : 'Giriş yapıp al' }}
            </LoadingButton>
            <LoadingButton
              v-else
              class="btn-ghost"
              :pending="action.isPending(`wl-${type.id}`)"
              pending-label="Sıraya ekleniyor…"
              @click="joinWaitlist(type)"
            >
              Tükendi · bekleme listesine gir
            </LoadingButton>
          </template>
          <span v-else class="badge badge-muted">
            {{ (type.remainingQuantity ?? 0) > 0 ? 'Satışta' : 'Tükendi' }}
          </span>
        </div>
      </article>
    </div>

    <!-- Açıklama -->
    <section class="panel" style="margin-top: 1.6rem">
      <h2 style="font-size: 1.15rem; margin-bottom: 0.6rem">Etkinlik hakkında</h2>
      <p style="margin: 0; color: var(--muted)">
        {{ event.description || 'Organizatör henüz açıklama eklemedi.' }}
      </p>
      <p class="event-card__meta" style="margin-top: 0.9rem">
        Bitiş: {{ formatDateTime(event.endDate) }} ·
        İptal/iade en geç etkinlikten {{ event.cancellationDeadlineHours }} saat önce.
      </p>
    </section>
  </article>
</template>
