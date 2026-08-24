import { OpenAPI } from '~/api-client'

export default defineNuxtPlugin(async () => {
  const config = useRuntimeConfig()
  const { accessToken } = useAuthCookies()
  const auth = useAuthStore()

  OpenAPI.BASE = import.meta.server
    ? ((config.apiBase as string) || config.public.apiBase)
    : config.public.apiBase
  OpenAPI.TOKEN = async () => accessToken.value ?? ''

  await auth.hydrate()
})
