<script setup lang="ts">
import type { NavigationMenuItem } from '@nuxt/ui'
import { useTemasekWebApiFeaturesEnabledFeaturesGetEndpoint } from '~/kubb'

const route = useRoute()

const { data } = useTemasekWebApiFeaturesEnabledFeaturesGetEndpoint()
const open = ref(false)

const links = computed(() => [
  [
    {
      label: 'Home',
      icon: 'i-lucide-house',
      to: '/',
      onSelect: () => {
        open.value = false
      },
    },
    ...(data.value?.isFacilitiesEnabled
      ? [
          {
            label: 'Facilities',
            icon: 'i-lucide-pin',
            to: '/facilities',
            defaultOpen: true,
            type: 'trigger' as const,
            onSelect: () => {
              open.value = false
            },
            children: [
              {
                label: 'Bookings',
                icon: 'i-lucide-calendar',
                to: '/facilities/bookings',
                onSelect: () => {
                  open.value = false
                },
              },
            ],
          },
        ]
      : []),
    {
      label: 'Settings',
      to: '/settings',
      icon: 'i-lucide-settings',
      defaultOpen: true,
      type: 'trigger' as const,
      children: [
        {
          label: 'Account',
          to: '/settings',
          exact: true,
          onSelect: () => {
            open.value = false
          },
        },
      ],
    },
    {
      label: 'Rooms',
      icon: 'i-lucide-door-open',
      to: '/rooms',
      onSelect: () => {
        open.value = false
      },
    },
  ],
  [
    {
      label: 'Feedback',
      icon: 'i-lucide-message-circle',
      to: 'https://t.me/qin_guan',
      target: '_blank',
    },
  ],
] satisfies NavigationMenuItem[][])

const groups = computed(() => [
  {
    id: 'links',
    label: 'Go to',
    items: links.value.flat(),
  },
  {
    id: 'code',
    label: 'Code',
    items: [
      {
        id: 'source',
        label: 'View page source',
        icon: 'i-simple-icons-github',
        to: `https://github.com/nuxt-ui-templates/dashboard/blob/main/app/pages${route.path === '/' ? '/index' : route.path}.vue`,
        target: '_blank',
      },
    ],
  },
])
</script>

<template>
  <UDashboardGroup unit="rem">
    <UDashboardSidebar
      id="default"
      v-model:open="open"
      collapsible
      resizable
      class="bg-elevated/25"
      :ui="{ footer: 'lg:border-t lg:border-default' }"
    >
      <template #default="{ collapsed }">
        <UDashboardSearchButton :collapsed="collapsed" class="bg-transparent ring-default" />

        <UNavigationMenu
          :collapsed="collapsed"
          :items="links[0]"
          orientation="vertical"
          tooltip
          popover
        />

        <UNavigationMenu
          :collapsed="collapsed"
          :items="links[1]"
          orientation="vertical"
          tooltip
          class="mt-auto"
        />
      </template>
    </UDashboardSidebar>

    <UDashboardSearch :groups="groups" />

    <slot />
  </UDashboardGroup>

  <ClerkLoaded>
    <Show when="signed-out">
      <div class="fixed inset-0 flex min-h-screen items-center justify-center bg-orange-50 p-4">
        <SignIn />
      </div>
    </Show>
  </ClerkLoaded>
</template>
