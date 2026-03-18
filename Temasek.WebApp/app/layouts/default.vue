<script setup lang="ts">
const { isSignedIn } = useAuth()
</script>

<template>
  <div :class="isSignedIn ? '' : 'invisible pointer-events-none'">
    <slot />
  </div>

  <ClerkLoaded>
    <Show when="signed-out">
      <div class="fixed inset-0 flex min-h-screen items-center justify-center bg-orange-50 p-4">
        <SignIn />
      </div>
    </Show>
  </ClerkLoaded>
</template>
