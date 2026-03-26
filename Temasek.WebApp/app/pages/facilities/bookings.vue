<script setup lang="ts">
</script>

<template>
  <UDashboardPanel id="facilities-booking">
    <template #header>
      <UDashboardNavbar title="Bookings" :ui="{ right: 'gap-3' }">
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
