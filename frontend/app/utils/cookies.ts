export const ACCESS_COOKIE = 'eventhub.access'
export const REFRESH_COOKIE = 'eventhub.refresh'

export function useAuthCookies() {
  const config = useRuntimeConfig()
  // Secure yalnız HTTPS dağıtımda; yerel Docker HTTP'de cookie düşmesin.
  const secure = String(config.public.apiBase ?? '').startsWith('https:')

  const accessToken = useCookie<string | null>(ACCESS_COOKIE, {
    sameSite: 'strict',
    secure,
    path: '/',
    // Access kısa ömürlü; otomatik yenileme zaten var.
    maxAge: 60 * 15,
    watch: true
  })

  const refreshToken = useCookie<string | null>(REFRESH_COOKIE, {
    sameSite: 'strict',
    secure,
    path: '/',
    maxAge: 60 * 60 * 24 * 7,
    watch: true
  })

  function clearAuthCookies() {
    accessToken.value = null
    refreshToken.value = null
  }

  return { accessToken, refreshToken, clearAuthCookies }
}
