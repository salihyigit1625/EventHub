<script setup lang="ts">
import type {
  CurrentUserDto,
  GateStaffProfileDto,
  GlobalStatsDto,
  OrganizerProfileDto,
  PagedEventListItemDto,
  PagedTicketDto,
  PagedWaitlistDto,
  WalletBalanceDto
} from '~/api-client'
import { organizerProfileFormSchema } from '~/schemas/profiles'
import { ROLE_LABEL } from '~/utils/catalog'

definePageMeta({ middleware: ['auth'] })

const api = useApi()
const auth = useAuthStore()
const notice = useNoticeStore()
const action = useAsyncAction()
const form = useZodForm(organizerProfileFormSchema)

const NuxtLinkComponent = resolveComponent('NuxtLink')

const { data: me, pending, refresh } = await useAsyncData(
  'profile-me',
  () => api.get<CurrentUserDto>('/api/auth/me'),
  { lazy: true }
)

const { data: organizer, pending: organizerPending, refresh: refreshOrganizer } = await useAsyncData(
  'profile-organizer',
  async () => {
    if (!auth.isOrganizer) return null
    return api.get<OrganizerProfileDto>('/api/organizers/me')
  },
  { watch: [() => auth.isOrganizer], lazy: true }
)

const { data: gate, pending: gatePending } = await useAsyncData(
  'profile-gate',
  async () => {
    if (!auth.isGateStaff) return null
    return api.get<GateStaffProfileDto>('/api/gate-staff/assigned-event')
  },
  { watch: [() => auth.isGateStaff], lazy: true }
)

const { data: wallet } = await useAsyncData(
  'profile-wallet',
  async () => {
    if (!auth.isAttendee) return null
    return api.get<WalletBalanceDto>('/api/wallet')
  },
  { watch: [() => auth.isAttendee], lazy: true }
)

/** Rozet sayaçları; her uç nokta yalnız totalCount için tek kayıt ister. */
const { data: attendeeCounts } = await useAsyncData(
  'profile-attendee-counts',
  async () => {
    if (!auth.isAttendee) return null
    const [tickets, waitlist] = await Promise.all([
      api.get<PagedTicketDto>('/api/tickets/mine', { page: 1, pageSize: 1 }),
      api.get<PagedWaitlistDto>('/api/waitlist/mine', { page: 1, pageSize: 1 })
    ])
    return { tickets: tickets.totalCount ?? 0, waitlist: waitlist.totalCount ?? 0 }
  },
  { watch: [() => auth.isAttendee], lazy: true }
)

const { data: myEvents } = await useAsyncData(
  'profile-my-events',
  async () => {
    if (!auth.isOrganizer) return null
    return api.get<PagedEventListItemDto>('/api/events/mine', { page: 1, pageSize: 1 })
  },
  { watch: [() => auth.isOrganizer], lazy: true }
)

const { data: adminStats } = await useAsyncData(
  'profile-admin-stats',
  async () => {
    if (!auth.isAdmin) return null
    return api.get<GlobalStatsDto>('/api/admin/stats')
  },
  { watch: [() => auth.isAdmin], lazy: true }
)

const companyName = ref('')
const taxNumber = ref('')

watch(organizer, (value) => {
  companyName.value = value?.companyName ?? ''
  taxNumber.value = value?.taxNumber ?? ''
}, { immediate: true })

const display = computed(() => me.value ?? auth.user)

const roleLabels = computed(() =>
  (display.value?.roles ?? []).map(role => ROLE_LABEL[role] ?? role)
)

const initials = computed(() => {
  const name = display.value?.fullName?.trim() || '?'
  const parts = name.split(/\s+/).filter(Boolean)
  if (parts.length >= 2)
    return `${parts[0]!.charAt(0)}${parts[1]!.charAt(0)}`.toLocaleUpperCase('tr')
  return name.slice(0, 2).toLocaleUpperCase('tr')
})

/** Kapak degradesi ana sayfadaki hero tonlarıyla aynı aileden gelir. */
const tone = computed(() => {
  if (auth.isAttendee) return 'attendee'
  if (auth.isOrganizer) return 'organizer'
  if (auth.isAdmin) return 'admin'
  if (auth.isGateStaff) return 'gate'
  return 'guest'
})

const primaryAction = computed(() => {
  if (auth.isAttendee) return { label: 'Biletlerim', to: '/account/tickets' }
  if (auth.isOrganizer) return { label: 'Stüdyoyu aç', to: '/organizer' }
  if (auth.isAdmin) return { label: 'Konsola git', to: '/admin' }
  if (auth.isGateStaff) return { label: 'Check-in aç', to: '/check-in' }
  return { label: 'Etkinlikleri keşfet', to: '/' }
})

interface StatTile {
  label: string
  value: string
  hint?: string
  to?: string
  tone?: 'gold'
}

const stats = computed<StatTile[]>(() => {
  const tiles: StatTile[] = []

  if (auth.isAttendee) {
    tiles.push(
      { label: 'Cüzdan bakiyesi', value: formatMoney(wallet.value?.balance), hint: 'Cüzdanı aç', to: '/account/wallet', tone: 'gold' },
      { label: 'Biletlerim', value: String(attendeeCounts.value?.tickets ?? 0), hint: 'Tümünü gör', to: '/account/tickets' },
      { label: 'Bekleme listesi', value: String(attendeeCounts.value?.waitlist ?? 0), hint: 'Sıradaki kayıtlar', to: '/account/waitlist' }
    )
  }

  if (auth.isOrganizer) {
    tiles.push(
      { label: 'Etkinliklerim', value: String(myEvents.value?.totalCount ?? 0), hint: 'Stüdyoda yönet', to: '/organizer' },
      { label: 'Başvuru durumu', value: organizer.value?.isApproved ? 'Onaylı' : 'Onay bekliyor' },
      { label: 'Şirket', value: organizer.value?.companyName || '—' }
    )
  }

  if (auth.isAdmin) {
    tiles.push(
      { label: 'Toplam kullanıcı', value: String(adminStats.value?.totalUsers ?? 0) },
      { label: 'Yayındaki etkinlik', value: String(adminStats.value?.publishedEvents ?? 0) },
      { label: 'Bekleyen onay', value: String(adminStats.value?.pendingOrganizers ?? 0), hint: 'Onaylara git', to: '/admin/approvals', tone: 'gold' }
    )
  }

  if (auth.isGateStaff) {
    tiles.push(
      { label: 'Atanan etkinlik', value: gate.value?.assignedEventTitle || 'Henüz atanmadı', hint: 'Check-in aç', to: '/check-in' },
      { label: 'Görev durumu', value: gate.value?.assignedEventId ? 'Hazır' : 'Bekliyor' }
    )
  }

  if (!tiles.length) {
    tiles.push(
      { label: 'Rol', value: roleLabels.value.join(' · ') || '—' },
      { label: 'Hesap durumu', value: display.value?.isActive ? 'Aktif' : 'Pasif' }
    )
  }

  return tiles
})

const quickLinks = computed(() => {
  const links: { label: string, text: string, to: string }[] = []

  if (auth.isAttendee) {
    links.push(
      { label: 'Hesap özeti', text: 'Bilet, cüzdan ve bekleme listesi', to: '/account' },
      { label: 'Biletlerim', text: 'Karekodlu biletlerini görüntüle', to: '/account/tickets' },
      { label: 'Cüzdan', text: 'Bakiye yükle, hareketleri incele', to: '/account/wallet' }
    )
  }
  if (auth.isOrganizer) {
    links.push(
      { label: 'Stüdyo', text: 'Etkinliklerini ve biletlerini yönet', to: '/organizer' },
      { label: 'Yeni etkinlik', text: 'Sahne kur, bilet tiplerini tanımla', to: '/organizer/events/new' }
    )
  }
  if (auth.isAdmin) {
    links.push(
      { label: 'Yönetim konsolu', text: 'Platform metrikleri ve kadro', to: '/admin' },
      { label: 'Organizatör onayları', text: 'Bekleyen başvuruları değerlendir', to: '/admin/approvals' }
    )
  }
  if (auth.isGateStaff) {
    links.push({ label: 'Check-in ekranı', text: 'Karekod okut, girişleri doğrula', to: '/check-in' })
  }

  links.push({ label: 'Etkinlik vitrini', text: 'Yayındaki tüm sahneleri keşfet', to: '/' })
  return links
})

async function copyEmail() {
  const email = display.value?.email
  if (!email) return
  try {
    await navigator.clipboard.writeText(email)
    notice.flash('E-posta adresi kopyalandı.')
  }
  catch {
    notice.fail('Kopyalanamadı', 'Tarayıcı panoya erişime izin vermedi.')
  }
}

async function saveOrganizer() {
  const parsed = form.parse({
    companyName: companyName.value,
    taxNumber: taxNumber.value
  })
  if (!parsed) return

  await action.run('save', async () => {
    try {
      await api.put<OrganizerProfileDto>('/api/organizers/me', {
        companyName: parsed.companyName,
        taxNumber: parsed.taxNumber || null
      })
      notice.flash('Şirket bilgilerin güncellendi.')
      await refreshOrganizer()
    }
    catch (error) {
      form.fromApi(error)
      notice.fail('Profil kaydedilemedi', toApiError(error).detail || toApiError(error).title)
    }
  })
}

async function reload() {
  await Promise.all([refresh(), auth.isOrganizer ? refreshOrganizer() : Promise.resolve()])
}

async function onLogout() {
  await auth.logout()
  notice.flash('Çıkış yaptın.')
  await navigateTo('/')
}
</script>

<template>
  <section class="profile">
    <AppSkeleton v-if="pending && !display" variant="block" :count="2" />

    <template v-else-if="display">
      <!-- ——— Kapak ——— -->
      <header class="profile-cover" :class="`profile-cover--${tone}`">
        <div class="profile-cover__aura" aria-hidden="true" />
        <div class="profile-cover__grain" aria-hidden="true" />

        <div class="profile-cover__id">
          <div class="profile-avatar">
            <span class="profile-avatar__initials" aria-hidden="true">{{ initials }}</span>
            <span
              class="profile-avatar__state"
              :class="{ 'profile-avatar__state--off': !display.isActive }"
              :title="display.isActive ? 'Aktif hesap' : 'Pasif hesap'"
            />
          </div>

          <div class="profile-cover__copy">
            <p class="profile-cover__eyebrow">Hesabım</p>
            <h1 class="profile-cover__name">{{ display.fullName || 'İsimsiz hesap' }}</h1>

            <button class="profile-mailto" type="button" title="E-postayı kopyala" @click="copyEmail">
              <span>{{ display.email }}</span>
              <span class="profile-mailto__icon" aria-hidden="true">⧉</span>
            </button>

            <div class="profile-cover__roles">
              <span v-for="label in roleLabels" :key="label" class="tone-pill">{{ label }}</span>
              <span class="tone-pill tone-pill--state">
                {{ display.isActive ? 'Aktif' : 'Pasif' }}
              </span>
            </div>
          </div>
        </div>

        <div class="profile-cover__side">
          <NuxtLink class="btn btn-accent" :to="primaryAction.to">{{ primaryAction.label }}</NuxtLink>
          <button class="btn btn-glass" type="button" :disabled="pending" @click="reload">
            Yenile
          </button>
        </div>
      </header>

      <!-- ——— Rol sayaçları ——— -->
      <div class="profile-stats">
        <component
          :is="tile.to ? NuxtLinkComponent : 'div'"
          v-for="tile in stats"
          :key="tile.label"
          class="stat-tile"
          :class="[{ 'stat-tile--link': tile.to }, tile.tone ? `stat-tile--${tile.tone}` : '']"
          v-bind="tile.to ? { to: tile.to } : {}"
        >
          <span class="stat-tile__label">{{ tile.label }}</span>
          <strong class="stat-tile__value">{{ tile.value }}</strong>
          <span v-if="tile.hint" class="stat-tile__hint">{{ tile.hint }} →</span>
        </component>
      </div>

      <!-- ——— İçerik ——— -->
      <div class="profile-layout">
        <div class="profile-main">
          <section class="panel profile-card">
            <div class="profile-card__head">
              <h2>Hesap bilgileri</h2>
              <span class="status" :class="display.isActive ? 'status--ok' : 'status--bad'">
                {{ display.isActive ? 'Aktif' : 'Pasif' }}
              </span>
            </div>

            <dl class="profile-dl">
              <div>
                <dt>Ad soyad</dt>
                <dd>{{ display.fullName || '—' }}</dd>
              </div>
              <div>
                <dt>E-posta</dt>
                <dd>{{ display.email || '—' }}</dd>
              </div>
              <div>
                <dt>Kullanıcı no</dt>
                <dd class="profile-mono">#{{ display.userId }}</dd>
              </div>
              <div>
                <dt>Roller</dt>
                <dd>{{ roleLabels.join(' · ') || '—' }}</dd>
              </div>
            </dl>

            <p class="profile-note">
              Ad ve e-posta kayıt sırasında belirlenir; şu an API üzerinden değiştirilemiyor.
            </p>
          </section>

          <!-- Organizatör -->
          <section v-if="auth.isOrganizer" class="panel profile-card">
            <div class="profile-card__head">
              <h2>Organizatör profili</h2>
              <span
                v-if="organizer"
                class="status"
                :class="organizer.isApproved ? 'status--ok' : 'status--warn'"
              >
                {{ organizer.isApproved ? 'Onaylı' : 'Onay bekliyor' }}
              </span>
            </div>

            <AppSkeleton v-if="organizerPending && !organizer" variant="row" :count="2" />
            <template v-else>
              <p class="profile-note">
                <template v-if="organizer?.approvedAt">
                  Onay tarihi: {{ formatDateTime(organizer.approvedAt) }}
                </template>
                <template v-else>
                  Başvurun yönetici onayından sonra etkinlik yayınlayabilirsin.
                </template>
              </p>

              <p v-if="form.formError" class="notice error">{{ form.formError }}</p>

              <div class="form-stack">
                <AppField label="Şirket adı" :error="form.errors.companyName">
                  <input v-model="companyName" type="text" maxlength="200" autocomplete="organization">
                </AppField>
                <AppField label="Vergi no (opsiyonel)" :error="form.errors.taxNumber">
                  <input v-model="taxNumber" type="text" maxlength="50">
                </AppField>

                <LoadingButton
                  class="btn-primary profile-save"
                  :pending="action.isPending('save')"
                  pending-label="Kaydediliyor…"
                  @click="saveOrganizer"
                >
                  Şirket bilgisini kaydet
                </LoadingButton>
              </div>
            </template>
          </section>

          <!-- Kapı görevlisi -->
          <section v-if="auth.isGateStaff" class="panel profile-card">
            <div class="profile-card__head">
              <h2>Kapı görevi</h2>
              <span class="status" :class="gate?.assignedEventId ? 'status--ok' : 'status--warn'">
                {{ gate?.assignedEventId ? 'Görev atandı' : 'Atama bekliyor' }}
              </span>
            </div>

            <AppSkeleton v-if="gatePending && !gate" variant="row" :count="2" />
            <template v-else>
              <dl class="profile-dl">
                <div>
                  <dt>Atanan etkinlik</dt>
                  <dd>{{ gate?.assignedEventTitle || 'Henüz atanmadı' }}</dd>
                </div>
                <div v-if="gate?.assignedEventId">
                  <dt>Etkinlik no</dt>
                  <dd class="profile-mono">#{{ gate.assignedEventId }}</dd>
                </div>
              </dl>
              <NuxtLink class="btn btn-primary profile-save" to="/check-in">
                Check-in ekranını aç
              </NuxtLink>
            </template>
          </section>

          <!-- Admin -->
          <section v-if="auth.isAdmin" class="panel profile-card">
            <div class="profile-card__head">
              <h2>Yönetici yetkileri</h2>
            </div>
            <p class="profile-note">
              Platform metrikleri, organizatör onayları ve kapı görevlisi atamaları yönetim konsolunda.
            </p>
            <dl class="profile-dl">
              <div>
                <dt>Satılan bilet</dt>
                <dd>{{ adminStats?.ticketsSold ?? 0 }}</dd>
              </div>
              <div>
                <dt>Toplam ciro</dt>
                <dd>{{ formatMoney(adminStats?.totalRevenue) }}</dd>
              </div>
            </dl>
          </section>
        </div>

        <!-- ——— Yan sütun ——— -->
        <aside class="profile-aside">
          <section class="panel profile-card">
            <div class="profile-card__head">
              <h2>Hızlı erişim</h2>
            </div>
            <nav class="quick-list">
              <NuxtLink
                v-for="link in quickLinks"
                :key="link.to"
                class="quick-item"
                :to="link.to"
              >
                <span class="quick-item__copy">
                  <strong>{{ link.label }}</strong>
                  <small>{{ link.text }}</small>
                </span>
                <span class="quick-item__arrow" aria-hidden="true">→</span>
              </NuxtLink>
            </nav>
          </section>

          <section class="panel profile-card profile-card--exit">
            <div class="profile-card__head">
              <h2>Oturum</h2>
            </div>
            <p class="profile-note">
              Ortak bir cihazdaysan işin bittiğinde oturumu kapatmayı unutma.
            </p>
            <button class="btn btn-outline profile-save" type="button" @click="onLogout">
              Oturumu kapat
            </button>
          </section>
        </aside>
      </div>
    </template>

    <AppEmpty
      v-else
      icon="◎"
      title="Profil yüklenemedi"
      text="Oturum bilgilerin alınamadı. Yenilemeyi dene veya yeniden giriş yap."
    >
      <button class="btn btn-primary" type="button" @click="reload">Tekrar dene</button>
    </AppEmpty>
  </section>
</template>
