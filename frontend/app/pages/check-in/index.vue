<script setup lang="ts">
import type { CheckInResultDto, GateStaffProfileDto } from '~/api-client'

definePageMeta({ layout: 'checkin', middleware: ['gate'] })

interface ScanLog {
  code: string
  ok: boolean
  at: string
  reason?: string | null
}

const api = useApi()
const notice = useNoticeStore()
const action = useAsyncAction()

const uniqueCode = ref('')
const deviceLocation = ref('')
const result = ref<CheckInResultDto | null>(null)
const log = ref<ScanLog[]>([])
const codeInput = ref<HTMLInputElement | null>(null)

/** Kütük yalnız son 8 satırı tutar; oturum toplamları ayrı sayılır. */
const sessionScans = ref(0)
const sessionAccepted = ref(0)

const { data: assigned } = await useAsyncData(
  'assigned-event',
  () => api.get<GateStaffProfileDto>('/api/gate-staff/assigned-event')
)

/* ——— Kamera ile karekod okuma ——— */
const video = ref<HTMLVideoElement | null>(null)
const cameraOn = ref(false)
const cameraError = ref('')
const detectorSupported = ref(false)

let stream: MediaStream | null = null
let detector: { detect: (source: CanvasImageSource) => Promise<{ rawValue: string }[]> } | null = null
let loopId: number | null = null
let lastSeen = ''
let lastSeenAt = 0

onMounted(() => {
  detectorSupported.value = 'BarcodeDetector' in window
  codeInput.value?.focus()
})

async function startCamera() {
  cameraError.value = ''
  if (!detectorSupported.value) {
    cameraError.value = 'Bu tarayıcı karekod okumayı desteklemiyor. Kodu elle gir.'
    return
  }
  try {
    stream = await navigator.mediaDevices.getUserMedia({
      video: { facingMode: 'environment' }
    })
    cameraOn.value = true
    await nextTick()
    if (video.value) {
      video.value.srcObject = stream
      await video.value.play()
    }
    const Detector = (window as unknown as {
      BarcodeDetector: new (options: { formats: string[] }) => typeof detector
    }).BarcodeDetector
    detector = new Detector({ formats: ['qr_code'] })
    loop()
  }
  catch {
    cameraOn.value = false
    cameraError.value = 'Kameraya erişilemedi. Kod alanını kullanabilirsin.'
  }
}

function stopCamera() {
  if (loopId !== null) cancelAnimationFrame(loopId)
  loopId = null
  stream?.getTracks().forEach(track => track.stop())
  stream = null
  detector = null
  cameraOn.value = false
}

async function loop() {
  if (!cameraOn.value || !detector || !video.value) return
  try {
    const found = await detector.detect(video.value)
    const raw = found[0]?.rawValue?.trim()
    const now = Date.now()
    // Aynı kodu saniyede bir kez işle: kamera tek karekodu sürekli görür.
    if (raw && (raw !== lastSeen || now - lastSeenAt > 2500)) {
      lastSeen = raw
      lastSeenAt = now
      uniqueCode.value = raw
      await scan()
    }
  }
  catch {
    // Tek karelik okuma hatası akışı durdurmaz.
  }
  loopId = requestAnimationFrame(loop)
}

onBeforeUnmount(stopCamera)

/* ——— Okutma ——— */
async function scan() {
  const code = uniqueCode.value.trim()
  if (!code) {
    notice.fail('Kod boş', 'Karekodu okut ya da bilet kodunu yaz.')
    return
  }

  await action.run('scan', async () => {
    try {
      const response = await api.post<CheckInResultDto>('/api/gate-staff/check-in', {
        uniqueCode: code,
        deviceLocation: deviceLocation.value || null
      })
      result.value = response
      sessionScans.value += 1
      if (response.isSuccessful) sessionAccepted.value += 1
      log.value = [
        {
          code: response.scannedCode ?? code,
          ok: Boolean(response.isSuccessful),
          at: response.checkedInAt ?? new Date().toISOString(),
          reason: response.failureReason
        },
        ...log.value
      ].slice(0, 8)

      if (!response.isSuccessful)
        notice.fail('Giriş reddedildi', response.failureReason || 'Bilet kabul edilmedi.')
    }
    catch (error) {
      const apiError = toApiError(error)
      sessionScans.value += 1
      result.value = {
        isSuccessful: false,
        scannedCode: code,
        failureReason: apiError.detail || apiError.title,
        checkedInAt: new Date().toISOString()
      }
      notice.fail('Okutma başarısız', apiError.detail || apiError.title)
    }
    finally {
      uniqueCode.value = ''
    }
  })
}

function next() {
  result.value = null
  lastSeen = ''
  nextTick(() => codeInput.value?.focus())
}

</script>

<template>
  <main class="kiosk-main">
    <GateEventHeader :profile="assigned" :scans="sessionScans" :accepted="sessionAccepted" />

    <!-- Sonuç varken tarayıcı yerine tam ekran karar kartı -->
    <Transition name="fade-slide" mode="out-in">
      <section v-if="result" key="verdict">
        <div class="verdict" :class="result.isSuccessful ? 'verdict--ok' : 'verdict--bad'">
          <div class="verdict__icon" aria-hidden="true">{{ result.isSuccessful ? '✓' : '✕' }}</div>
          <h2 class="verdict__title">
            {{ result.isSuccessful ? 'GİRİŞ BAŞARILI' : 'GEÇERSİZ BİLET' }}
          </h2>
          <p style="margin: 0; font-size: 1.02rem">
            {{ result.isSuccessful ? `Bilet #${result.ticketId} kabul edildi.` : result.failureReason }}
          </p>
          <p class="verdict__meta">
            {{ result.scannedCode }} · {{ formatDateTime(result.checkedInAt) }}
          </p>
        </div>
        <button class="btn btn-scan" type="button" style="margin-top: 0.9rem" @click="next">
          Sonraki bileti okut
        </button>
      </section>

      <section v-else key="scanner" style="display:grid; gap:0.9rem">
        <div class="scanner">
          <template v-if="cameraOn">
            <video ref="video" playsinline muted />
            <div class="scanner__frame" />
            <div class="scanner__line" />
          </template>
          <div v-else class="scanner__idle">
            <div class="scanner__idle-icon" aria-hidden="true">▣</div>
            <strong>Kamerayı başlat</strong>
            <span>{{ cameraError || 'Karekodu çerçeveye getir, otomatik okunur.' }}</span>
            <button class="btn btn-ghost" type="button" @click="startCamera">Kamerayı aç</button>
          </div>
        </div>

        <button v-if="cameraOn" class="btn btn-ghost" type="button" @click="stopCamera">
          Kamerayı kapat
        </button>

        <form class="kiosk-form" @submit.prevent="scan">
          <div class="field kiosk-input">
            <span>Bilet kodu</span>
            <input
              ref="codeInput"
              v-model="uniqueCode"
              type="text"
              maxlength="64"
              autocomplete="off"
              autocapitalize="characters"
              spellcheck="false"
              placeholder="KOD"
            >
          </div>
          <LoadingButton
            class="btn-scan"
            type="submit"
            :pending="action.isPending('scan')"
            pending-label="Kontrol ediliyor…"
          >
            Kontrol et
          </LoadingButton>
          <div class="field kiosk-location">
            <span>Konum (opsiyonel)</span>
            <input
              v-model="deviceLocation"
              type="text"
              maxlength="200"
              placeholder="Örn. Ana giriş"
            >
          </div>
        </form>
      </section>
    </Transition>

    <section v-if="log.length" class="kiosk-log">
      <p class="eyebrow">Son okutmalar</p>
      <div
        v-for="(row, index) in log"
        :key="`${row.code}-${index}`"
        class="kiosk-log__row"
        :class="row.ok ? 'kiosk-log__row--ok' : 'kiosk-log__row--bad'"
      >
        <span>{{ row.ok ? '✓' : '✕' }} {{ row.code }}</span>
        <span>{{ row.reason || formatDateTime(row.at) }}</span>
      </div>
    </section>
  </main>
</template>
