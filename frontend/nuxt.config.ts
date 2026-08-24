import { fileURLToPath } from 'node:url'

export default defineNuxtConfig({
  compatibilityDate: '2026-08-24',
  srcDir: 'app',
  modules: ['@pinia/nuxt'],
  css: [
    '~/assets/css/main.css',
    '~/assets/css/system.css',
    '~/assets/css/theme.css',
    '~/assets/css/ui.css'
  ],
  typescript: {
    strict: true,
    typeCheck: false,
    tsConfig: {
      compilerOptions: {
        noUncheckedIndexedAccess: true
      }
    }
  },
  imports: {
    dirs: ['stores', 'schemas', 'utils']
  },
  runtimeConfig: {
    // SSR inside Docker talks to the API service name
    apiBase: process.env.NUXT_API_BASE || process.env.NUXT_PUBLIC_API_BASE || 'http://localhost:5170',
    public: {
      // Browser calls the published API host
      apiBase: process.env.NUXT_PUBLIC_API_BASE || 'http://localhost:5170'
    }
  },
  app: {
    pageTransition: { name: 'fade-slide', mode: 'out-in' },
    layoutTransition: { name: 'layout-fade', mode: 'out-in' },
    head: {
      htmlAttrs: { lang: 'tr' },
      title: 'EventHub',
      meta: [
        { name: 'viewport', content: 'width=device-width, initial-scale=1' },
        {
          name: 'description',
          content: 'Etkinlik keşfet, bilet al, cüzdanını yönet. Organizatörler için yayın ve bilet yönetimi.'
        },
        { name: 'theme-color', content: '#0b1220' }
      ],
      link: [
        { rel: 'icon', type: 'image/svg+xml', href: '/favicon.svg' },
        { rel: 'icon', type: 'image/png', sizes: '32x32', href: '/brand/favicon-32.png' },
        { rel: 'apple-touch-icon', href: '/brand/apple-touch-icon.png' },
        { rel: 'preconnect', href: 'https://fonts.googleapis.com' },
        { rel: 'preconnect', href: 'https://fonts.gstatic.com', crossorigin: '' },
        {
          rel: 'stylesheet',
          href: 'https://fonts.googleapis.com/css2?family=DM+Sans:ital,opsz,wght@0,9..40,400;0,9..40,500;0,9..40,600;0,9..40,700;1,9..40,400&family=Fraunces:opsz,wght@9..144,500;9..144,600;9..144,700&display=swap'
        }
      ]
    }
  },
  alias: {
    '#api-client': fileURLToPath(new URL('./app/api-client', import.meta.url))
  }
})
