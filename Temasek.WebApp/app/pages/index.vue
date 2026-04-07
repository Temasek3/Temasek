<script setup lang="ts">
import { useTemasekWebApiFeaturesEnabledFeaturesGetEndpoint } from '~/kubb'

const _ = useTemasekWebApiFeaturesEnabledFeaturesGetEndpoint()
</script>

<template>
  <UDashboardPanel id="index">
    <template #header>
      <UDashboardNavbar title="Temasek3" :ui="{ right: 'gap-3' }">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="flex flex-col w-full flex-1">
        Temasek!
      </div>
    </template>
  </UDashboardPanel>
</template>
