<script setup lang="ts">
import type { PagedEventListItemDto, PagedTicketDto, WalletBalanceDto } from '~/api-client'

const api = useApi()
const auth = useAuthStore()

/* ——— Filtre durumu ——— */
type RangeKey = 'all' | 'today' | 'week' | 'month'

const search = ref('')
const debouncedSearch = ref('')
const range = ref<RangeKey>('all')
const venue = ref('')
const page = ref(1)

const RANGES: { key: RangeKey, label: string }[] = [
  { key: 'all', label: 'Tüm tarihler' },
  { key: 'today', label: 'Bugün' },
  { key: 'week', label: 'Bu hafta' },
  { key: 'month', label: 'Bu ay' }
]

function rangeBounds(key: RangeKey) {
  if (key === 'all') return { from: undefined, to: undefined }
  const from = new Date()
  const to = new Date()
  if (key === 'today') to.setHours(23, 59, 59, 999)
  else to.setDate(to.getDate() + (key === 'week' ? 7 : 30))
  return { from: from.toISOString(), to: to.toISOString() }
}

const query = computed(() => {
  const { from, to } = rangeBounds(range.value)
  return {
    page: page.value,
    pageSize: 12,
    search: debouncedSearch.value || undefined,
    venue: venue.value || undefined,
    from,
    to
  }
})

let searchTimer: ReturnType<typeof setTimeout> | null = null
watch(search, (value) => {
  if (searchTimer) clearTimeout(searchTimer)
  searchTimer = setTimeout(() => {
    debouncedSearch.value = value.trim()
    page.value = 1
  }, 320)
})

watch([range, venue], () => {
  page.value = 1
})

const { data, pending, error, refresh } = await useAsyncData(
  'home-events',
  () => api.get<PagedEventListItemDto>('/api/events', query.value),
  { watch: [query], lazy: true }
)

/* ——— Vitrin ——— */
const items = computed(() => data.value?.items ?? [])

const upcoming = computed(() =>
  [...items.value].sort(
    (a, b) => new Date(a.startDate ?? 0).getTime() - new Date(b.startDate ?? 0).getTime()
  )
)

const spotlight = computed(() => upcoming.value[0] ?? null)
/** Vitrinde ana afişin yanında duran iki küçük kapak. */
const teasers = computed(() => upcoming.value.slice(1, 3))
const rail = computed(() => upcoming.value.slice(0, 8))

/** Fiyat ve doluluk liste uç noktasında yok; kartlar için istemcide tamamlanır. */
const { facets } = useEventFacets(() => items.value.map(item => item.id))
const spotlightFacet = computed(() => (spotlight.value?.id ? facets.value[spotlight.value.id] ?? null : null))

function posterFor(id?: number, version?: number | null) {
  if (!id) return ''
  return `${api.posterUrl(id)}?v=${version ?? 0}`
}

const spotlightPoster = computed(() =>
  spotlight.value?.posterDocumentId
    ? posterFor(spotlight.value.id, spotlight.value.posterDocumentId)
    : ''
)

const spotlightPrice = computed(() => {
  const price = spotlightFacet.value?.minPrice
  if (price === null || price === undefined) return ''
  return price <= 0 ? 'Ücretsiz' : `${formatMoney(price)}'den başlıyor`
})

/** Mekan çipleri filtre uygulanmamışken toplanır, daraldıkça kaybolmasın. */
const venuePool = ref<string[]>([])
watch(items, (list) => {
  if (venue.value) return
  const unique = [...new Set(list.map(item => item.venue).filter(Boolean) as string[])]
  if (unique.length) venuePool.value = unique.slice(0, 6)
}, { immediate: true })

const hasFilters = computed(() => Boolean(debouncedSearch.value || venue.value || range.value !== 'all'))

function clearFilters() {
  search.value = ''
  debouncedSearch.value = ''
  venue.value = ''
  range.value = 'all'
  page.value = 1
}

function toggleVenue(value: string) {
  venue.value = venue.value === value ? '' : value
}

function scrollToResults() {
  document.getElementById('kesfet')?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

/* ——— Vitrin sayaçları (filtreden bağımsız) ——— */
const { data: totals } = await useAsyncData(
  'home-totals',
  async () => {
    const week = rangeBounds('week')
    const [all, soon] = await Promise.all([
      api.get<PagedEventListItemDto>('/api/events', { page: 1, pageSize: 1 }),
      api.get<PagedEventListItemDto>('/api/events', { page: 1, pageSize: 1, from: week.from, to: week.to })
    ])
    return { published: all.totalCount ?? 0, thisWeek: soon.totalCount ?? 0 }
  },
  { lazy: true, default: () => ({ published: 0, thisWeek: 0 }) }
)

/* ——— Katılımcı oturum şeridi ——— */
const { data: wallet } = await useAsyncData(
  'home-wallet',
  async () => (auth.isAttendee ? api.get<WalletBalanceDto>('/api/wallet') : null),
  { watch: [() => auth.isAttendee], lazy: true }
)

const { data: myTickets } = await useAsyncData(
  'home-my-tickets',
  async () => (auth.isAttendee ? api.get<PagedTicketDto>('/api/tickets/mine', { page: 1, pageSize: 3 }) : null),
  { watch: [() => auth.isAttendee], lazy: true }
)

/* ——— Role göre karşılama ——— */
const hero = computed(() => {
  if (!auth.isAuthenticated) {
    return {
      eyebrow: 'Canlı etkinlik vitrini',
      title: 'Şehrin sahnesi\nbir tık uzakta.',
      text: 'Yayındaki konserleri, tiyatroları ve sahne gecelerini keşfet. Beğendiğini seç, giriş yap, biletini saniyeler içinde al.',
      tone: 'guest' as const
    }
  }
  if (auth.isAttendee) {
    return {
      eyebrow: `Hoş geldin, ${auth.fullName}`,
      title: 'Sıradaki gecen\nseni bekliyor.',
      text: 'Cüzdanın hazır. Etkinliği seç, bileti al, karekodunu kapıda okut.',
      tone: 'attendee' as const
    }
  }
  if (auth.isOrganizer) {
    return {
      eyebrow: `Organizatör · ${auth.fullName}`,
      title: 'Sahneyi sen kur,\nvitrini biz taşıyalım.',
      text: 'Aşağıdaki katalog yayındaki her etkinliği gösterir. Kendi programını stüdyodan yönetirsin.',
      tone: 'organizer' as const
    }
  }
  if (auth.isAdmin) {
    return {
      eyebrow: `Yönetici · ${auth.fullName}`,
      title: 'Platformun\nvitrin görünümü.',
      text: 'Yayındaki etkinlikleri katılımcının gördüğü gibi izle. Metrikler ve onaylar konsolda.',
      tone: 'admin' as const
    }
  }
  return {
    eyebrow: `Kapı görevlisi · ${auth.fullName}`,
    title: 'Kapı ekibi\nhazır.',
    text: 'Programı buradan görebilirsin. Giriş kontrolü için check-in ekranını aç.',
    tone: 'gate' as const
  }
})
</script>

<template>
  <section>
    <!-- ——— Vitrin ——— -->
    <section class="showcase" :class="`showcase--${hero.tone}`">
      <div class="showcase__aura" aria-hidden="true" />
      <div class="showcase__grain" aria-hidden="true" />

      <div class="showcase__copy">
        <p class="showcase__eyebrow">
          <span class="showcase__dot" aria-hidden="true" />
          {{ hero.eyebrow }}
        </p>
        <h1 class="showcase__title">{{ hero.title }}</h1>
        <p class="showcase__text">{{ hero.text }}</p>

        <form class="hero-search" role="search" @submit.prevent="scrollToResults">
          <span class="hero-search__icon" aria-hidden="true">⌕</span>
          <input
            v-model="search"
            type="search"
            placeholder="Etkinlik, sanatçı ya da mekan ara…"
            aria-label="Etkinlik ara"
          >
          <span v-if="pending" class="hero-search__spinner" aria-hidden="true" />
          <button class="btn btn-accent hero-search__go" type="submit">Ara</button>
        </form>

        <div class="hero-quick" role="group" aria-label="Hızlı tarih filtresi">
          <button
            v-for="option in RANGES"
            :key="option.key"
            class="hero-chip"
            :class="{ 'hero-chip--on': range === option.key }"
            type="button"
            @click="range = option.key"
          >
            {{ option.label }}
          </button>
          <button v-if="hasFilters" class="hero-chip hero-chip--clear" type="button" @click="clearFilters">
            Temizle ×
          </button>
        </div>

        <div class="showcase__actions">
          <template v-if="!auth.isAuthenticated">
            <NuxtLink class="btn btn-accent btn-lg" to="/register">Ücretsiz kayıt ol</NuxtLink>
          </template>
          <template v-else-if="auth.isAttendee">
            <NuxtLink class="btn btn-accent btn-lg" to="/account/tickets">Biletlerim</NuxtLink>
          </template>
          <template v-else-if="auth.isOrganizer">
            <NuxtLink class="btn btn-accent btn-lg" to="/organizer/events/new">Yeni etkinlik</NuxtLink>
          </template>
          <template v-else-if="auth.isAdmin">
            <NuxtLink class="btn btn-accent btn-lg" to="/admin">Konsolu aç</NuxtLink>
          </template>
          <template v-else>
            <NuxtLink class="btn btn-accent btn-lg" to="/check-in">Check-in aç</NuxtLink>
          </template>

          <button class="btn btn-glass btn-lg" type="button" @click="scrollToResults">
            Tüm programı gör
          </button>
        </div>

        <dl class="showcase__stats">
          <div>
            <dt>Yayında etkinlik</dt>
            <dd>{{ totals.published }}</dd>
          </div>
          <div>
            <dt>Bu hafta sahne</dt>
            <dd>{{ totals.thisWeek }}</dd>
          </div>
          <div>
            <dt>Bilet</dt>
            <dd>Anında</dd>
          </div>
        </dl>
      </div>

      <!-- Öne çıkan etkinlik vitrini -->
      <div class="showcase__gallery">
        <NuxtLink v-if="spotlight" class="spotlight" :to="`/events/${spotlight.id}`">
          <img
            v-if="spotlightPoster"
            class="spotlight__img"
            :src="spotlightPoster"
            :alt="`${spotlight.title} afişi`"
          >
          <div v-else class="spotlight__img spotlight__img--blank" aria-hidden="true" />
          <div class="spotlight__veil" aria-hidden="true" />
          <div class="spotlight__content">
            <span class="spotlight__tag">Öne çıkan</span>
            <strong class="spotlight__title">{{ spotlight.title }}</strong>
            <span class="spotlight__meta">
              {{ spotlight.venue }} · {{ formatDateTime(spotlight.startDate) }}
            </span>
            <span v-if="spotlightPrice" class="spotlight__price">{{ spotlightPrice }}</span>
            <span class="spotlight__cta">Bileti incele →</span>
          </div>
        </NuxtLink>

        <div v-else class="spotlight spotlight--empty">
          <div class="spotlight__content">
            <span class="spotlight__tag">Vitrin</span>
            <strong class="spotlight__title">Yakında yeni sahneler</strong>
            <span class="spotlight__meta">Yayına alınan ilk etkinlik burada görünecek.</span>
          </div>
        </div>

        <div v-if="teasers.length" class="teaser-row">
          <NuxtLink
            v-for="event in teasers"
            :key="`t-${event.id}`"
            class="teaser"
            :to="`/events/${event.id}`"
          >
            <img
              v-if="event.posterDocumentId"
              class="teaser__img"
              :src="posterFor(event.id, event.posterDocumentId)"
              :alt="`${event.title} afişi`"
              loading="lazy"
            >
            <div v-else class="teaser__img teaser__img--blank" aria-hidden="true" />
            <div class="teaser__veil" aria-hidden="true" />
            <span class="teaser__title">{{ event.title }}</span>
            <span class="teaser__date">{{ formatDay(event.startDate) }} {{ formatMonth(event.startDate) }}</span>
          </NuxtLink>
        </div>
      </div>
    </section>

    <!-- ——— Role özel şerit ——— -->
    <div v-if="auth.isAttendee" class="session-strip session-strip--attendee">
      <div class="session-strip__item">
        <span>Cüzdan</span>
        <strong>{{ formatMoney(wallet?.balance) }}</strong>
      </div>
      <div class="session-strip__item">
        <span>Biletlerim</span>
        <strong>{{ myTickets?.totalCount ?? 0 }}</strong>
      </div>
      <div class="session-strip__item session-strip__item--wide">
        <span>Son bilet</span>
        <strong v-if="myTickets?.items?.[0]">
          <NuxtLink :to="`/account/tickets/${myTickets.items[0].uniqueCode}`">
            {{ myTickets.items[0].uniqueCode }}
          </NuxtLink>
        </strong>
        <strong v-else>Henüz yok</strong>
      </div>
      <NuxtLink class="btn btn-soft" to="/account">Cüzdanı aç</NuxtLink>
    </div>

    <div v-else-if="auth.isOrganizer" class="session-strip session-strip--organizer">
      <div class="session-strip__copy">
        <strong>Organizatör modu</strong>
        <span>Bu sayfa herkese açık kataloğu gösterir. Yönetim için stüdyoya geç.</span>
      </div>
      <NuxtLink class="btn btn-primary" to="/organizer">Stüdyoyu aç</NuxtLink>
    </div>

    <div v-else-if="auth.isAdmin" class="session-strip session-strip--admin">
      <div class="session-strip__copy">
        <strong>Yönetici görünümü</strong>
        <span>Katalog salt okunur. Onay ve kadro işlemleri konsolda.</span>
      </div>
      <NuxtLink class="btn btn-primary" to="/admin">Konsola git</NuxtLink>
    </div>

    <div v-else-if="auth.isGateStaff" class="session-strip session-strip--organizer">
      <div class="session-strip__copy">
        <strong>Kapı görevlisi</strong>
        <span>Programı inceleyebilirsin. Giriş kontrolü check-in ekranından yapılır.</span>
      </div>
      <NuxtLink class="btn btn-primary" to="/check-in">Check-in aç</NuxtLink>
    </div>

    <!-- ——— Keşif ——— -->
    <div id="kesfet" class="discover">
      <div class="discover__row">
        <span class="discover__count">
          <strong>{{ data?.totalCount ?? 0 }}</strong> etkinlik
          <template v-if="debouncedSearch"> · “{{ debouncedSearch }}”</template>
          <template v-if="range !== 'all'"> · {{ RANGES.find(r => r.key === range)?.label }}</template>
        </span>

        <div v-if="venuePool.length" class="discover__chips">
          <span class="discover__label">Mekan</span>
          <button
            v-for="name in venuePool"
            :key="name"
            class="chip"
            :class="{ 'chip--on': venue === name }"
            type="button"
            @click="toggleVenue(name)"
          >
            {{ name }}
          </button>
          <button v-if="hasFilters" class="chip chip--clear" type="button" @click="clearFilters">
            Filtreleri temizle ×
          </button>
        </div>
      </div>
    </div>

    <!-- ——— Sonuçlar ——— -->
    <AppSkeleton
      v-if="pending && !items.length"
      class="event-grid"
      :stack="false"
      variant="card"
      :count="6"
    />

    <div v-else-if="error" class="empty-state">
      <div class="empty-state__icon" aria-hidden="true">!</div>
      <p class="empty-state__title">Etkinlikler yüklenemedi</p>
      <p class="empty-state__text">Sunucuya ulaşılamıyor. Bağlantını kontrol edip tekrar dene.</p>
      <button class="btn btn-primary" type="button" @click="refresh()">Tekrar dene</button>
    </div>

    <template v-else-if="items.length">
      <template v-if="!hasFilters && rail.length > 2">
        <div class="section-head">
          <div>
            <h2>Yaklaşanlar</h2>
            <p class="event-card__meta">Tarihi en yakın sahneler</p>
          </div>
          <span class="pill">Kaydır →</span>
        </div>
        <div class="rail rail--featured" aria-label="Yaklaşan etkinlikler">
          <EventCard
            v-for="event in rail"
            :key="`f-${event.id}`"
            :event="event"
            :facet="event.id ? facets[event.id] : null"
            featured
          />
        </div>
      </template>

      <div class="section-head">
        <div>
          <h2>{{ hasFilters ? 'Arama sonuçları' : 'Tüm etkinlikler' }}</h2>
          <p class="event-card__meta">
            {{ data?.totalCount ?? 0 }} etkinlik
            <template v-if="venue"> · {{ venue }}</template>
          </p>
        </div>
        <button v-if="hasFilters" class="btn btn-ghost" type="button" @click="clearFilters">
          Filtreleri temizle
        </button>
      </div>

      <div class="event-grid" :class="{ 'event-grid--loading': pending }">
        <EventCard
          v-for="event in items"
          :key="event.id"
          :event="event"
          :facet="event.id ? facets[event.id] : null"
        />
      </div>

      <AppPager
        :page="data?.page ?? 1"
        :page-size="data?.pageSize ?? 12"
        :total-count="data?.totalCount ?? 0"
        @change="page = $event"
      />
    </template>

    <AppEmpty
      v-else-if="hasFilters"
      icon="⌕"
      title="Aramanla eşleşen etkinlik yok"
      text="Farklı bir tarih aralığı ya da mekan deneyebilir, aramayı temizleyebilirsin."
    >
      <button class="btn btn-primary" type="button" @click="clearFilters">Filtreleri temizle</button>
    </AppEmpty>

    <AppEmpty
      v-else
      icon="◎"
      title="Şu an yayında etkinlik yok"
      text="Organizatörler yeni sahneler hazırlıyor. Yayına alınan ilk etkinlik burada görünecek."
    >
      <NuxtLink v-if="!auth.isAuthenticated" class="btn btn-primary" to="/register/organizer">
        Organizatör olarak katıl
      </NuxtLink>
    </AppEmpty>
  </section>
</template>
