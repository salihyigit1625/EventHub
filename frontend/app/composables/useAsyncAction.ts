type ActionKey = string | number

/**
 * Aynı anda tek işlem çalıştırır; ikinci tıklama sessizce yok sayılır.
 * Kontenjan/bilet gibi yarış koşullu uçlarda çift istek engellenir.
 */
export function useAsyncAction() {
  const activeKey = ref<ActionKey | null>(null)

  const pending = computed(() => activeKey.value !== null)

  function isPending(key: ActionKey) {
    return activeKey.value === key
  }

  async function run<T>(key: ActionKey, fn: () => Promise<T>): Promise<T | undefined> {
    if (activeKey.value !== null) return undefined
    activeKey.value = key
    try {
      return await fn()
    }
    finally {
      activeKey.value = null
    }
  }

  return { activeKey, pending, isPending, run }
}
