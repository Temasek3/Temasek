// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
    compatibilityDate: 'latest',
    devtools: { enabled: true },
    ssr: false,

    modules: [
        '@nuxt/a11y',
        '@nuxt/content',
        '@nuxt/hints',
        '@nuxt/image',
        '@nuxt/scripts',
        '@nuxt/test-utils',
        '@nuxt/ui',
        '@clerk/nuxt'
    ],

    css: ['~/assets/css/main.css'],

    content: {
        experimental: {
            sqliteConnector: 'native'
        }
    },

    app: {
        head: {
            title: 'Temasek3'
        }
    },

    clerk: {
        skipServerMiddleware: true,
        publishableKey: process.env.NUXT_PUBLIC_CLERK_PUBLISHABLE_KEY
    }
})