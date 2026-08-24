<script setup lang="ts">
import type { DocumentDto, OrganizerProfileDto, PagedOrganizerProfileDto } from '~/api-client'

definePageMeta({ layout: 'admin', middleware: ['admin'] })

const api = useApi()
const notice = useNoticeStore()
const action = useAsyncAction()

const page = ref(1)
const selected = ref<OrganizerProfileDto | null>(null)
const docs = ref<DocumentDto[]>([])
const docsPending = ref(false)
const previews = ref<Record<number, string>>({})

const { data, pending, refresh } = await useAsyncData(
  'pending-organizers',
  () => api.get<PagedOrganizerProfileDto>('/api/admin/organizers/pending', { page: page.value, pageSize: 10 }),
  { watch: [page], lazy: true }
)

function revokePreviews() {
  for (const url of Object.values(previews.value)) URL.revokeObjectURL(url)
  previews.value = {}
}

async function inspect(row: OrganizerProfileDto) {
  selected.value = row
  docs.value = []
  revokePreviews()
  docsPending.value = true
  try {
    const all = await api.get<DocumentDto[]>('/api/documents')
    docs.value = all.filter(doc => doc.uploadedByUserId === row.userId)

    // Görsel ve PDF belgeleri modal içinde göstermek için indir.
    await Promise.all(
      docs.value.map(async (doc) => {
        if (!doc.id) return
        if (!/^image\/|application\/pdf/.test(doc.contentType ?? '')) return
        try {
          const blob = await api.download(`/api/documents/${doc.id}`)
          previews.value = { ...previews.value, [doc.id]: URL.createObjectURL(blob) }
        }
        catch {
          // Önizleme başarısızsa indirme bağlantısı yine çalışır.
        }
      })
    )
  }
  catch (error) {
    notice.fail('Belgeler alınamadı', toApiError(error).detail)
  }
  finally {
    docsPending.value = false
  }
}

function close() {
  selected.value = null
  revokePreviews()
}

onBeforeUnmount(revokePreviews)

async function approve(row: OrganizerProfileDto) {
  if (!row.userId) return
  await action.run(row.userId, async () => {
    try {
      await api.post<OrganizerProfileDto>(`/api/admin/organizers/${row.userId}/approve`)
      notice.flash(`${row.companyName} onaylandı.`)
      close()
      await refresh()
    }
    catch (error) {
      const apiError = toApiError(error)
      notice.fail('Onaylanamadı', apiError.detail || apiError.title)
    }
  })
}

async function download(doc: DocumentDto) {
  if (!doc.id) return
  try {
    const blob = await api.download(`/api/documents/${doc.id}`)
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = doc.originalFileName || `belge-${doc.id}`
    link.click()
    URL.revokeObjectURL(url)
  }
  catch (error) {
    notice.fail('İndirilemedi', toApiError(error).detail)
  }
}

const rows = computed(() => data.value?.items ?? [])
</script>

<template>
  <section>
    <div class="section-head" style="margin-top: 0">
      <div>
        <h1 style="font-size: 1.6rem">Organizatör onayları</h1>
        <p class="event-card__meta">Belgeleri incele, tek tıkla karar ver.</p>
      </div>
      <span class="pill">{{ data?.totalCount ?? 0 }} bekliyor</span>
    </div>

    <AppSkeleton v-if="pending && !data" variant="row" :count="4" />

    <p v-else-if="!rows.length" class="empty">Bekleyen başvuru yok.</p>

    <table v-else class="table table--compact">
      <thead>
        <tr>
          <th>Şirket</th>
          <th>Başvuran</th>
          <th>Vergi no</th>
          <th>Durum</th>
          <th />
        </tr>
      </thead>
      <tbody>
        <tr v-for="row in rows" :key="row.userId">
          <td><strong>{{ row.companyName }}</strong></td>
          <td>
            {{ row.fullName }}
            <div class="event-card__meta">{{ row.email }}</div>
          </td>
          <td>{{ row.taxNumber || '—' }}</td>
          <td><span class="status status--warn">Bekliyor</span></td>
          <td>
            <div style="display:flex; gap:0.4rem; flex-wrap:wrap; justify-content:flex-end">
              <button class="btn btn-ghost" type="button" @click="inspect(row)">
                Belgeler
              </button>
              <LoadingButton
                class="btn-primary"
                :pending="action.isPending(row.userId ?? -1)"
                pending-label="Onaylanıyor…"
                @click="approve(row)"
              >
                Onayla
              </LoadingButton>
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

    <AppModal
      :open="Boolean(selected)"
      :title="selected?.companyName || 'Başvuru'"
      :subtitle="`${selected?.fullName ?? ''} · ${selected?.email ?? ''}`"
      @close="close"
    >
      <div class="form-stack">
        <div class="kpi-grid">
          <div class="kpi">
            <span class="kpi__label">Vergi no</span>
            <span class="kpi__value" style="font-size: 1.15rem">{{ selected?.taxNumber || '—' }}</span>
          </div>
          <div class="kpi">
            <span class="kpi__label">Kullanıcı</span>
            <span class="kpi__value" style="font-size: 1.15rem">#{{ selected?.userId }}</span>
          </div>
        </div>

        <h3 style="font-size: 1rem">Yüklenen belgeler</h3>

        <AppSkeleton v-if="docsPending" variant="block" :count="2" />

        <p v-else-if="!docs.length" class="empty" style="padding: 0.8rem 0">
          Bu başvuruya bağlı belge bulunamadı.
        </p>

        <article v-for="doc in docs" v-else :key="doc.id" class="form-stack" style="gap: 0.5rem">
          <div style="display:flex; justify-content:space-between; gap:1rem; align-items:center">
            <div>
              <strong>{{ doc.originalFileName }}</strong>
              <div class="event-card__meta">
                {{ doc.contentType }} · {{ formatBytes(doc.fileSizeInBytes) }}
              </div>
            </div>
            <button class="btn btn-ghost" type="button" @click="download(doc)">İndir</button>
          </div>
          <div class="doc-preview">
            <img
              v-if="doc.id && previews[doc.id] && doc.contentType?.startsWith('image/')"
              :src="previews[doc.id]"
              :alt="doc.originalFileName"
            >
            <iframe
              v-else-if="doc.id && previews[doc.id]"
              :src="previews[doc.id]"
              :title="doc.originalFileName"
              style="height: 24rem"
            />
            <span v-else class="event-card__meta">Önizleme yok</span>
          </div>
        </article>
      </div>

      <template #footer>
        <button
          class="btn btn-ghost"
          type="button"
          disabled
          title="Backend'de reddetme ucu bulunmuyor"
        >
          Reddet
        </button>
        <LoadingButton
          class="btn-primary"
          :pending="action.isPending(selected?.userId ?? -1)"
          pending-label="Onaylanıyor…"
          @click="selected && approve(selected)"
        >
          Onayla
        </LoadingButton>
      </template>
    </AppModal>
  </section>
</template>
