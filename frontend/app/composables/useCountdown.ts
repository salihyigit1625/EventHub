import type { MaybeRefOrGetter } from 'vue'

export interface Countdown {
  days: number
  hours: number
  minutes: number
  seconds: number
}

const SECOND = 1000
const MINUTE = 60 * SECOND
const HOUR = 60 * MINUTE
const DAY = 24 * HOUR

/** Hedef tarihe kalan süreyi saniyede bir günceller. SSR'da ilk değeri hesaplar. */
export function useCountdown(target: MaybeRefOrGetter<string | Date | null | undefined>) {
  const now = ref(Date.now())
  let timer: ReturnType<typeof setInterval> | null = null

  const targetTime = computed(() => {
    const raw = toValue(target)
    if (!raw) return null
    const date = raw instanceof Date ? raw : new Date(raw)
    return Number.isNaN(date.getTime()) ? null : date.getTime()
  })

  const remaining = computed(() => {
    if (targetTime.value === null) return null
    return Math.max(0, targetTime.value - now.value)
  })

  const parts = computed<Countdown>(() => {
    const ms = remaining.value ?? 0
    return {
      days: Math.floor(ms / DAY),
      hours: Math.floor((ms % DAY) / HOUR),
      minutes: Math.floor((ms % HOUR) / MINUTE),
      seconds: Math.floor((ms % MINUTE) / SECOND)
    }
  })

  const isPast = computed(() => remaining.value !== null && remaining.value === 0)
  const isSoon = computed(() => remaining.value !== null && remaining.value > 0 && remaining.value < 3 * DAY)

  onMounted(() => {
    now.value = Date.now()
    timer = setInterval(() => {
      now.value = Date.now()
    }, SECOND)
  })

  onBeforeUnmount(() => {
    if (timer) clearInterval(timer)
  })

  return { parts, remaining, isPast, isSoon }
}
