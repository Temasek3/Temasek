<script setup lang="ts">
</script>

<template>
  <UDashboardPanel id="facilities">
    <template #header>
      <UDashboardNavbar title="Facilities" :ui="{ right: 'gap-3' }">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <div class="flex flex-col w-full flex-1" />
    </template>
  </UDashboardPanel>
</template>
