<script setup lang="ts">
import type {
  TemasekWebApiFeaturesRoomsCreateEndpointMutationRequest as CreateRoomPayload,
  TemasekWebApiFeaturesRoomsListResponse as RoomSummary,
} from '~/kubb'
import PixelPalEasterEgg from '~/components/easter-eggs/PixelPalEasterEgg.vue'
import {
  useTemasekWebApiFeaturesRoomsCreateEndpoint,
  useTemasekWebApiFeaturesRoomsListEndpoint,
} from '~/kubb'

const roomId = ref('')
const roomName = ref('')
const createError = ref('')
const route = useRoute()

const roomsQuery = useTemasekWebApiFeaturesRoomsListEndpoint({
  client: {
    auth: 'none',
  },
  query: {
    retry: false,
    refetchOnWindowFocus: false,
  },
})
const createRoomMutation = useTemasekWebApiFeaturesRoomsCreateEndpoint({
  client: {
    auth: 'none',
  },
  mutation: {
    onSuccess: async () => {
      roomId.value = ''
      roomName.value = ''
      await roomsQuery.refetch()
    },
    onError: () => {
      createError.value = 'Unable to create this room. Check if the Room ID already exists.'
    },
  },
})

const normalizedRoomId = computed(() => roomId.value.trim())
const isCreating = computed(() => createRoomMutation.isPending.value)
const isLoadingRooms = computed(() => roomsQuery.isFetching.value)
const loadError = computed(() => roomsQuery.isError.value
  ? 'Unable to load rooms right now. Please try again.'
  : '')
const rooms = computed<RoomSummary[]>(() => {
  const response = roomsQuery.data.value ?? []

  return [...response].sort((a, b) => a.name.localeCompare(b.name) || a.roomId.localeCompare(b.roomId))
})
const canCreateRoom = computed(() => normalizedRoomId.value.length > 0 && !isCreating.value)
const shouldTriggerPixelPal = computed(() => {
  const queryValue = route.query.pixelPal
  const normalized = Array.isArray(queryValue) ? queryValue[0] : queryValue

  return normalized === '1' || normalized === 'true' || normalized === 'yes'
})

function formatUpdatedAt(updatedAtUtc: string) {
  const parsed = new Date(updatedAtUtc)
  if (Number.isNaN(parsed.getTime())) {
    return 'Unknown update time'
  }

  return parsed.toLocaleString([], {
    hour: '2-digit',
    minute: '2-digit',
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

function createRoom() {
  if (!canCreateRoom.value) {
    return
  }

  createError.value = ''

  const payload: CreateRoomPayload = {
    roomId: normalizedRoomId.value,
  }

  if (roomName.value.trim()) {
    payload.name = roomName.value.trim()
  }

  createRoomMutation.mutate({
    data: payload,
  })
}
</script>

<template>
  <UDashboardPanel id="rooms">
    <template #header>
      <UDashboardNavbar title="Rooms" :ui="{ right: 'gap-3' }">
        <template #leading>
          <UDashboardSidebarCollapse />
        </template>
      </UDashboardNavbar>
    </template>

    <template #body>
      <PixelPalEasterEgg :trigger="shouldTriggerPixelPal" />

      <div class="grid gap-6 xl:grid-cols-[minmax(18rem,24rem)_1fr]">
        <UCard>
          <template #header>
            <h2 class="text-base font-semibold">
              Create Room
            </h2>
          </template>

          <form class="grid gap-3" @submit.prevent="createRoom">
            <UFormField label="Room ID" required>
              <UInput
                v-model="roomId"
                autocomplete="off"
                placeholder="e.g. room-01"
              />
            </UFormField>

            <UFormField label="Display Name (optional)">
              <UInput
                v-model="roomName"
                autocomplete="off"
                placeholder="e.g. War Room"
              />
            </UFormField>

            <UAlert
              v-if="createError"
              color="error"
              variant="soft"
              :description="createError"
            />

            <div>
              <UButton type="submit" :loading="isCreating" :disabled="!canCreateRoom">
                Create Room
              </UButton>
            </div>
          </form>
        </UCard>

        <UCard>
          <template #header>
            <div class="flex items-center justify-between gap-3">
              <h2 class="text-base font-semibold">
                All Rooms
              </h2>

              <UButton color="neutral" variant="soft" size="sm" :loading="isLoadingRooms" @click="roomsQuery.refetch()">
                Refresh
              </UButton>
            </div>
          </template>

          <UAlert
            v-if="loadError"
            color="error"
            variant="soft"
            :description="loadError"
          />

          <div v-else-if="isLoadingRooms" class="text-sm text-muted">
            Loading rooms...
          </div>

          <div v-else-if="rooms.length === 0" class="text-sm text-muted">
            No rooms yet. Create your first room.
          </div>

          <div v-else class="grid gap-3 md:grid-cols-2 2xl:grid-cols-3">
            <UCard
              v-for="room in rooms"
              :key="room.roomId"
              class="grid gap-3"
            >
              <div class="grid gap-1">
                <h3 class="font-semibold leading-tight">
                  {{ room.name }}
                </h3>
                <p class="text-xs text-muted">
                  {{ room.roomId }}
                </p>
              </div>

              <div class="text-sm text-muted">
                {{ room.scheduleCount }} activities
              </div>

              <div class="text-sm text-muted">
                Updated {{ formatUpdatedAt(room.updatedAtUtc) }}
              </div>

              <div class="flex gap-2 pt-1">
                <UButton :to="`/rooms/${encodeURIComponent(room.roomId)}`" size="sm">
                  View
                </UButton>
                <UButton :to="`/rooms/${encodeURIComponent(room.roomId)}/edit`" color="neutral" variant="soft" size="sm">
                  Edit
                </UButton>
              </div>
            </UCard>
          </div>
        </UCard>
      </div>
    </template>
  </UDashboardPanel>
</template>
