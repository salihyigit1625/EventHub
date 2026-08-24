import type { AuthResponseDto } from '~/api-client'

type QueryValue = string | number | boolean | null | undefined
type Query = Record<string, QueryValue>

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
  body?: unknown
  query?: Query
  headers?: Record<string, string>
  responseType?: 'json' | 'blob' | 'text'
}

const AUTH_SKIP = ['/api/auth/login', '/api/auth/refresh', '/api/auth/register']

/** İstek başına tutulur; SSR'de modül kapsamı tüm kullanıcılarca paylaşılır. */
interface AuthRefreshHolder {
  _authRefreshInFlight?: Promise<boolean> | null
}

function isAuthSkip(url: string) {
  return AUTH_SKIP.some(prefix => url.startsWith(prefix))
}

function hasStatus(error: unknown, status: number) {
  return typeof error === 'object'
    && error !== null
    && 'statusCode' in error
    && (error as { statusCode?: number }).statusCode === status
}

export function useApi() {
  const config = useRuntimeConfig()
  const { accessToken, refreshToken, clearAuthCookies } = useAuthCookies()

  const apiBase = import.meta.server
    ? (config.apiBase as string) || config.public.apiBase
    : config.public.apiBase

  const publicApiBase = config.public.apiBase

  const raw = $fetch.create({
    baseURL: apiBase,
    retry: 0,
    onRequest({ options }) {
      const headers = new Headers(options.headers as HeadersInit | undefined)
      if (accessToken.value)
        headers.set('Authorization', `Bearer ${accessToken.value}`)
      options.headers = headers
    }
  })

  async function refreshSession() {
    if (!refreshToken.value) return false

    // nuxtApp SSR'de istek başına kurulur; modül değişkeni tüm isteklerce paylaşılır.
    const holder = useNuxtApp() as unknown as AuthRefreshHolder
    if (!holder._authRefreshInFlight) {
      holder._authRefreshInFlight = (async () => {
        try {
          const next = await $fetch<AuthResponseDto>('/api/auth/refresh', {
            baseURL: apiBase,
            method: 'POST',
            body: { refreshToken: refreshToken.value }
          })
          if (!next.accessToken || !next.refreshToken) return false
          accessToken.value = next.accessToken
          refreshToken.value = next.refreshToken
          return true
        }
        catch {
          clearAuthCookies()
          return false
        }
        finally {
          holder._authRefreshInFlight = null
        }
      })()
    }
    return holder._authRefreshInFlight
  }

  async function request<T>(url: string, options: RequestOptions = {}) {
    const run = () => raw<T>(url, {
      method: options.method ?? 'GET',
      body: options.body as never,
      query: options.query as never,
      headers: options.headers,
      responseType: options.responseType as never
    })

    try {
      return await run()
    }
    catch (error) {
      if (hasStatus(error, 401) && !isAuthSkip(url)) {
        const recovered = await refreshSession()
        if (recovered) return await run()
        clearAuthCookies()
        if (import.meta.client) {
          const route = useRoute()
          await navigateTo(loginWithRedirect(route.fullPath))
        }
      }
      throw toApiError(error)
    }
  }

  return {
    request,
    get: <T>(url: string, query?: Query) => request<T>(url, { query }),
    post: <T>(url: string, body?: unknown) => request<T>(url, { method: 'POST', body }),
    put: <T>(url: string, body?: unknown) => request<T>(url, { method: 'PUT', body }),
    del: <T>(url: string) => request<T>(url, { method: 'DELETE' }),
    upload: <T>(url: string, file: File) => {
      const body = new FormData()
      body.append('file', file)
      return request<T>(url, { method: 'POST', body })
    },
    download: (url: string) => request<Blob>(url, { responseType: 'blob' }),
    refreshSession,
    // posters are loaded in the browser → use public base
    posterUrl: (eventId: number) => `${publicApiBase}/api/events/${eventId}/poster`
  }
}
