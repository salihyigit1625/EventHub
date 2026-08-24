<script setup lang="ts">
import type { DocumentDto } from '~/api-client'

definePageMeta({ layout: 'organizer', middleware: ['organizer'] })

const api = useApi()
const notice = useNoticeStore()
const file = ref<File | null>(null)

const { data, refresh } = await useAsyncData(
  'organizer-documents',
  () => api.get<DocumentDto[]>('/api/documents')
)

function onFile(e: Event) {
  file.value = (e.target as HTMLInputElement).files?.[0] ?? null
}

async function upload() {
  if (!file.value) return
  try {
    await api.upload<DocumentDto>('/api/documents', file.value)
    notice.flash('Belge yüklendi.')
    file.value = null
    await refresh()
  }
  catch (error) {
    notice.flash(toApiError(error).message, 'bad')
  }
}

async function download(doc: DocumentDto) {
  if (!doc.id) return
  try {
    const blob = await api.download(`/api/documents/${doc.id}`)
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = doc.originalFileName || `document-${doc.id}`
    link.click()
    URL.revokeObjectURL(url)
  }
  catch (error) {
    notice.flash(toApiError(error).message, 'bad')
  }
}
</script>

<template>
  <section>
    <h1>Belgeler</h1>
    <p class="event-card__meta">png, jpg, pdf — en fazla 10 MB</p>
    <form class="form-row" style="margin: 1rem 0" @submit.prevent="upload">
      <input type="file" accept=".png,.jpg,.jpeg,.pdf" @change="onFile">
      <div><button class="btn btn-primary" type="submit" :disabled="!file">Yükle</button></div>
    </form>
    <table class="table">
      <thead>
        <tr>
          <th>Dosya</th>
          <th>Boyut</th>
          <th>Tarih</th>
          <th />
        </tr>
      </thead>
      <tbody>
        <tr v-for="doc in data" :key="doc.id">
          <td>{{ doc.originalFileName }}</td>
          <td>{{ formatBytes(doc.fileSizeInBytes) }}</td>
          <td>{{ formatDateTime(doc.createdAt) }}</td>
          <td><button class="btn btn-ghost" type="button" @click="download(doc)">İndir</button></td>
        </tr>
      </tbody>
    </table>
  </section>
</template>
