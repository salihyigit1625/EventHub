export type NoticeTone = 'good' | 'bad' | 'info'

export interface Notice {
  id: number
  title: string
  description?: string
  tone: NoticeTone
}

let seq = 0

export const useNoticeStore = defineStore('notice', () => {
  const items = ref<Notice[]>([])
  const timers = new Map<number, ReturnType<typeof setTimeout>>()

  function dismiss(id: number) {
    items.value = items.value.filter(item => item.id !== id)
    const timer = timers.get(id)
    if (timer) {
      clearTimeout(timer)
      timers.delete(id)
    }
  }

  function push(notice: Omit<Notice, 'id'>, ms = 4200) {
    const id = ++seq
    items.value = [...items.value, { ...notice, id }].slice(-4)
    if (import.meta.client && ms > 0)
      timers.set(id, setTimeout(() => dismiss(id), ms))
    return id
  }

  function flash(title: string, tone: NoticeTone = 'good', ms = 4200) {
    return push({ title, tone }, ms)
  }

  /** Hatanın nedenini ayrı satırda gösterir. */
  function fail(title: string, description?: string, ms = 6000) {
    return push({ title, description, tone: 'bad' }, ms)
  }

  function clear() {
    for (const timer of timers.values()) clearTimeout(timer)
    timers.clear()
    items.value = []
  }

  return { items, push, flash, fail, dismiss, clear }
})
