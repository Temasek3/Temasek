import { dirname, join } from 'node:path'
import process from 'node:process'
import { fileURLToPath } from 'node:url'

const currentDir = dirname(fileURLToPath(import.meta.url))

// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: 'latest',
  devtools: { enabled: true },
  ssr: false,

  modules: [
    '@nuxt/a11y',
    '@nuxt/content',
    '@nuxt/image',
    '@nuxt/scripts',
    '@nuxt/test-utils',
    '@nuxt/ui',
    '@clerk/nuxt',
    '@nuxt/hints',
    '@vueuse/nuxt',
    (_, nuxt) => {
      nuxt.options.alias['#hints-config'] = join(currentDir, 'hints.config.ts')
    },
  ],

  css: ['~/assets/css/main.css'],

  runtimeConfig: {
    public: {
      temasekWebApiHttps: process.env.NUXT_PUBLIC_TEMASEK_WEBAPI_HTTPS ?? process.env.TEMASEK_WEBAPI_HTTPS ?? '',
    },
  },

  content: {
    experimental: {
      sqliteConnector: 'native',
    },
  },

  app: {
    head: {
      title: 'Temasek3',
    },
  },

  clerk: {
    skipServerMiddleware: true,
    publishableKey: process.env.NUXT_PUBLIC_CLERK_PUBLISHABLE_KEY,
  },

  typescript: {
    tsConfig: {
      compilerOptions: {
        allowImportingTsExtensions: true,
      },
    },
  },
})
