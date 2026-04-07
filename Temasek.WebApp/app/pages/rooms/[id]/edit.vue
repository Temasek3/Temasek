<script setup lang="ts">
import type {
  RoomSignboardSnapshot,
  SignboardActivity,
} from './signboard-helpers'
import type {
  TemasekWebApiFeaturesRoomsSignboardContractsRoomSignboardResponse as RoomSignboardSnapshotContract,
  TemasekWebApiFeaturesRoomsSignboardUpdateEndpointMutationRequest as RoomSignboardUpdateRequest,
} from '~/kubb'
import { useQueryClient } from '@tanstack/vue-query'

import {
  useTemasekWebApiFeaturesRoomsSignboardGetEndpoint,
  useTemasekWebApiFeaturesRoomsSignboardUpdateEndpoint,
} from '~/kubb'
import {
  createActivityId,
  formatEndsIn,
  getActivitiesAfterNext,
  getMetaItems,
  getScheduleState,
  getSeatingAlertMessage,
  getSuggestedEndTime,
  getSuggestedStartTime,
  hasPositiveDuration,
  isValidTimeText,
  normalizeActivity,
  normalizeSchedule,
  sortSchedule,
  validateScheduleArray,
} from './signboard-helpers'

interface SignboardFormState {
  title: string
  start: string
  end: string
  personnel: string
  location: string
}

definePageMeta({
  layout: 'rooms',
})

const route = useRoute()
const runtimeConfig = useRuntimeConfig()
const queryClient = useQueryClient()

const roomId = computed(() => String(route.params.id))
const encodedRoomId = computed(() => encodeURIComponent(roomId.value))
const apiBaseUrl = computed(() => String(runtimeConfig.public.temasekWebApiHttps ?? '').trim())
const roomState = ref<RoomSignboardSnapshot | null>(null)
const roomQuery = useTemasekWebApiFeaturesRoomsSignboardGetEndpoint(encodedRoomId, {
  client: {
    auth: 'none',
  },
  query: {
    retry: false,
    refetchOnWindowFocus: false,
  },
})
const roomUpdateMutation = useTemasekWebApiFeaturesRoomsSignboardUpdateEndpoint({
  client: {
    auth: 'none',
  },
})
const now = ref(new Date())
const editingActivityId = ref<string | null>(null)
const editorPanel = ref<'form' | 'json'>('form')
const isSaving = computed(() => roomUpdateMutation.isPending.value)
const saveError = ref('')
const saveSuccess = ref('')
const jsonEditorText = ref('[]')
const jsonEditorError = ref('')
const formDirty = ref(false)
const jsonDirty = ref(false)

const form = reactive<SignboardFormState>({
  title: '',
  start: '09:00',
  end: '10:00',
  personnel: '',
  location: '',
})

let tickIntervalId: number | null = null

const roomStreamUrl = computed(() => buildApiUrl(`${getSignboardPath(roomId.value)}/stream`))
const {
  data: roomStreamData,
  close: stopRoomStream,
  open: startRoomStream,
} = useEventSource<string[], RoomSignboardSnapshotContract>(
  roomStreamUrl,
  ['signboard'],
  {
    immediate: false,
    autoConnect: false,
    autoReconnect: {
      retries: -1,
      delay: 1000,
    },
    serializer: {
      read: value => JSON.parse(value ?? 'null') as RoomSignboardSnapshotContract,
    },
  },
)

const roomName = computed(() => roomState.value?.name?.trim() || roomId.value)
const displayPath = computed(() => `/rooms/${encodeURIComponent(roomId.value)}`)
const sortedSchedule = computed(() => sortSchedule(normalizeSchedule(roomState.value?.schedule)))
const updatedAtText = computed(() => {
  const updatedAtUtc = roomState.value?.updatedAtUtc
  if (!updatedAtUtc) {
    return 'Waiting for the latest update'
  }

  const parsed = new Date(updatedAtUtc)
  if (Number.isNaN(parsed.getTime())) {
    return 'Waiting for the latest update'
  }

  return `Updated ${parsed.toLocaleString([], {
    hour: '2-digit',
    minute: '2-digit',
    month: 'short',
    day: 'numeric',
  })}`
})

const clockTimeText = computed(() => (
  now.value.toLocaleTimeString([], {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  })
))

const scheduleCountText = computed(() => {
  const count = sortedSchedule.value.length
  return `${count} ${count === 1 ? 'activity' : 'activities'}`
})

const activeFormTitle = computed(() => editingActivityId.value ? 'Edit activity' : 'New activity')
const saveButtonLabel = computed(() => editingActivityId.value ? 'Save changes' : 'Add activity')
const syncStatusLabel = computed(() => {
  if (isSaving.value) {
    return 'Saving'
  }

  if (roomQuery.isFetching.value) {
    return 'Syncing'
  }

  return formDirty.value || jsonDirty.value ? 'Unsaved changes' : 'Live sync on'
})

const syncStatusColor = computed(() => {
  if (isSaving.value || formDirty.value || jsonDirty.value) {
    return 'warning'
  }

  return 'success'
})

const previewState = computed(() => getScheduleState(sortedSchedule.value, now.value))
const renderedCurrent = computed(() => previewState.value.current)
const renderedNext = computed(() => previewState.value.next)
const renderedLaterActivities = computed(() => getActivitiesAfterNext(sortedSchedule.value, renderedNext.value))
const showUpcomingAsPrimary = computed(() => !renderedCurrent.value && !!renderedNext.value)
const primaryActivity = computed(() => renderedCurrent.value || renderedNext.value || null)
const primaryLabel = computed(() => showUpcomingAsPrimary.value ? 'Next activity' : 'Current activity')
const primaryCountdownText = computed(() => formatEndsIn(primaryActivity.value, now.value))
const primaryMetaItems = computed(() => primaryActivity.value ? getMetaItems(primaryActivity.value) : [])
const seatingAlertMessage = computed(() => getSeatingAlertMessage(renderedNext.value, now.value))

function getSignboardPath(targetRoomId: string = roomId.value) {
  return `/rooms/${encodeURIComponent(targetRoomId)}/signboard`
}

function buildApiUrl(path: string) {
  return apiBaseUrl.value ? new URL(path, apiBaseUrl.value).toString() : path
}

function createEmptyForm(sourceSchedule: SignboardActivity[] = sortedSchedule.value): SignboardFormState {
  const start = getSuggestedStartTime(sourceSchedule)

  return {
    title: '',
    start,
    end: getSuggestedEndTime(start),
    personnel: '',
    location: '',
  }
}

function assignForm(next: SignboardFormState, dirty = false) {
  form.title = next.title
  form.start = next.start
  form.end = next.end
  form.personnel = next.personnel
  form.location = next.location
  formDirty.value = dirty
}

function resetJsonEditorText() {
  jsonEditorText.value = JSON.stringify(sortedSchedule.value, null, 2)
  jsonDirty.value = false
}

function setNewForm(clearFeedback = false) {
  editingActivityId.value = null
  assignForm(createEmptyForm(sortedSchedule.value), false)

  if (clearFeedback) {
    saveError.value = ''
    saveSuccess.value = ''
    jsonEditorError.value = ''
  }
}

function syncEditorDraftsFromState() {
  if (!jsonDirty.value) {
    resetJsonEditorText()
  }

  if (formDirty.value) {
    return
  }

  if (!editingActivityId.value) {
    assignForm(createEmptyForm(sortedSchedule.value), false)
    return
  }

  const activity = sortedSchedule.value.find(item => item.id === editingActivityId.value)
  if (!activity) {
    setNewForm(false)
    return
  }

  assignForm({
    title: activity.title,
    start: activity.start,
    end: activity.end,
    personnel: activity.personnel,
    location: activity.location,
  }, false)
}

function markFormDirty() {
  formDirty.value = true
  saveError.value = ''
  saveSuccess.value = ''
}

function prepareNewActivityForm() {
  setNewForm(true)
  editorPanel.value = 'form'
}

function editActivity(activityId: string) {
  const activity = sortedSchedule.value.find(item => item.id === activityId)
  if (!activity) {
    return
  }

  editingActivityId.value = activityId
  assignForm({
    title: activity.title,
    start: activity.start,
    end: activity.end,
    personnel: activity.personnel,
    location: activity.location,
  }, false)
  editorPanel.value = 'form'
  saveError.value = ''
  saveSuccess.value = ''
}

function openJsonEditor() {
  if (!jsonDirty.value) {
    resetJsonEditorText()
  }

  jsonEditorError.value = ''
  editorPanel.value = 'json'
}

function onJsonEditorUpdate(value: string | number) {
  jsonEditorText.value = String(value ?? '')
  jsonDirty.value = true
  jsonEditorError.value = ''
  saveError.value = ''
  saveSuccess.value = ''
}

function resetJsonEditor() {
  resetJsonEditorText()
  jsonEditorError.value = ''
  saveError.value = ''
  saveSuccess.value = ''
}

function applyRoomSnapshot(snapshot: Partial<RoomSignboardSnapshotContract> | null | undefined, targetRoomId: string = roomId.value) {
  roomState.value = {
    roomId: String(snapshot?.roomId ?? targetRoomId),
    name: String(snapshot?.name ?? targetRoomId),
    schedule: normalizeSchedule(snapshot?.schedule),
    updatedAtUtc: snapshot?.updatedAtUtc ? String(snapshot.updatedAtUtc) : undefined,
  }
}

function closeRoomStream() {
  stopRoomStream()
}

function openRoomStream(targetRoomId: string = roomId.value) {
  if (!import.meta.client || targetRoomId !== roomId.value) {
    return
  }

  startRoomStream()
}

function syncRoomChannel(targetRoomId: string = roomId.value) {
  closeRoomStream()

  if (targetRoomId === roomId.value) {
    openRoomStream(targetRoomId)
  }
}

async function saveSnapshot(nextSchedule: SignboardActivity[]) {
  saveError.value = ''
  jsonEditorError.value = ''

  try {
    const payload: RoomSignboardUpdateRequest = {
      name: roomState.value?.name ?? roomId.value,
      schedule: sortSchedule(nextSchedule),
    }

    const response = await roomUpdateMutation.mutateAsync({
      roomId: encodedRoomId.value,
      data: payload,
    })

    queryClient.setQueryData(roomQuery.queryKey, response)
    formDirty.value = false
    jsonDirty.value = false
    applyRoomSnapshot(response, roomId.value)
    saveSuccess.value = `Saved ${new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}`
    return true
  }
  catch {
    saveError.value = 'Unable to save signboard changes right now.'
    return false
  }
}

async function submitForm() {
  saveError.value = ''
  saveSuccess.value = ''
  jsonEditorError.value = ''

  if (!form.title.trim()) {
    saveError.value = 'Add a title before saving.'
    return
  }

  if (!isValidTimeText(form.start) || !isValidTimeText(form.end)) {
    saveError.value = 'Start and end times must use HH:mm.'
    return
  }

  if (!hasPositiveDuration(form.start, form.end)) {
    saveError.value = 'Each activity must end after it starts.'
    return
  }

  const activity = normalizeActivity({
    id: editingActivityId.value ?? createActivityId(),
    title: form.title,
    start: form.start,
    end: form.end,
    personnel: form.personnel,
    location: form.location,
  }, sortedSchedule.value.length)

  const nextSchedule = editingActivityId.value
    ? sortedSchedule.value.map(item => item.id === editingActivityId.value ? activity : item)
    : [...sortedSchedule.value, activity]

  const saved = await saveSnapshot(nextSchedule)
  if (saved) {
    editActivity(activity.id)
  }
}

async function removeActivity(activityId: string) {
  if (isSaving.value) {
    return
  }

  saveError.value = ''
  saveSuccess.value = ''
  jsonEditorError.value = ''

  const nextSchedule = sortedSchedule.value.filter(activity => activity.id !== activityId)
  const saved = await saveSnapshot(nextSchedule)

  if (saved && editingActivityId.value === activityId) {
    setNewForm(false)
  }
}

async function applyJsonEditorChanges() {
  saveError.value = ''
  saveSuccess.value = ''
  jsonEditorError.value = ''

  let parsed: unknown

  try {
    parsed = JSON.parse(jsonEditorText.value)
  }
  catch {
    jsonEditorError.value = 'JSON could not be parsed.'
    return
  }

  const validation = validateScheduleArray(parsed)
  if (!validation.ok) {
    jsonEditorError.value = validation.message
    return
  }

  const saved = await saveSnapshot(validation.normalized)
  if (saved) {
    editorPanel.value = 'form'
    setNewForm(false)
    resetJsonEditorText()
  }
}

watch(roomId, (nextRoomId) => {
  applyRoomSnapshot(null, nextRoomId)
  setNewForm(true)
  resetJsonEditorText()
  syncRoomChannel(nextRoomId)
}, {
  immediate: true,
})

watch(roomQuery.data, (snapshot) => {
  if (!snapshot) {
    return
  }

  if (snapshot.roomId && String(snapshot.roomId) !== roomId.value) {
    return
  }

  applyRoomSnapshot(snapshot, roomId.value)
}, {
  immediate: true,
})

watch(sortedSchedule, () => {
  syncEditorDraftsFromState()
}, {
  deep: true,
  immediate: true,
})

watch(roomStreamData, (snapshot) => {
  if (!snapshot) {
    return
  }

  if (snapshot.roomId && String(snapshot.roomId) !== roomId.value) {
    return
  }

  applyRoomSnapshot(snapshot, roomId.value)
})

onMounted(() => {
  tickIntervalId = window.setInterval(() => {
    now.value = new Date()
  }, 1000)
})

onBeforeUnmount(() => {
  closeRoomStream()

  if (tickIntervalId !== null) {
    window.clearInterval(tickIntervalId)
  }
})
</script>

<template>
  <main class="mx-auto max-w-7xl px-4 py-6 text-default sm:px-6 lg:px-8">
    <div class="flex flex-col gap-6">
      <section class="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div class="space-y-2">
          <div class="flex flex-wrap gap-2">
            <UBadge color="neutral" variant="subtle">
              Edit schedule
            </UBadge>
            <UBadge :color="syncStatusColor" variant="subtle">
              {{ syncStatusLabel }}
            </UBadge>
            <UBadge color="neutral" variant="soft">
              {{ scheduleCountText }}
            </UBadge>
          </div>

          <div class="space-y-1">
            <h1 class="text-3xl font-semibold tracking-tight text-highlighted sm:text-4xl">
              {{ roomName }}
            </h1>
            <p class="text-sm text-muted">
              {{ updatedAtText }}
            </p>
          </div>
        </div>

        <div class="flex flex-wrap gap-3">
          <UButton :to="displayPath" color="neutral" variant="subtle">
            View signboard
          </UButton>
          <UButton color="neutral" variant="subtle" :disabled="isSaving" @click="prepareNewActivityForm">
            New activity
          </UButton>
          <UButton :disabled="isSaving" @click="submitForm">
            {{ saveButtonLabel }}
          </UButton>
        </div>
      </section>

      <div
        v-if="saveError"
        class="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm font-medium text-red-700 dark:border-red-900/60 dark:bg-red-950/40 dark:text-red-300"
      >
        {{ saveError }}
      </div>

      <div
        v-else-if="saveSuccess"
        class="rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-medium text-emerald-700 dark:border-emerald-900/60 dark:bg-emerald-950/40 dark:text-emerald-300"
      >
        {{ saveSuccess }}
      </div>

      <div class="grid gap-6 xl:grid-cols-[minmax(0,1.15fr)_minmax(0,1fr)]">
        <UCard>
          <div class="space-y-4">
            <div class="flex items-center justify-between gap-3">
              <div>
                <h2 class="text-lg font-semibold text-highlighted">
                  Schedule
                </h2>
                <p class="text-sm text-muted">
                  Select an activity to edit, or create a new one.
                </p>
              </div>

              <UButton size="sm" color="neutral" variant="subtle" :disabled="isSaving" @click="prepareNewActivityForm">
                Add activity
              </UButton>
            </div>

            <div v-if="sortedSchedule.length" class="space-y-3">
              <div
                v-for="activity in sortedSchedule" :key="activity.id" class="rounded-lg border p-4"
                :class="activity.id === editingActivityId ? 'border-primary bg-primary/5 dark:bg-primary/10' : 'border-default bg-default'"
              >
                <div class="flex items-start justify-between gap-3">
                  <button class="min-w-0 flex-1 text-left" type="button" @click="editActivity(activity.id)">
                    <p class="truncate text-base font-semibold text-highlighted">
                      {{ activity.title || 'Untitled activity' }}
                    </p>
                    <p class="mt-1 text-sm font-medium tabular-nums text-muted">
                      {{ activity.start }} to {{ activity.end }}
                    </p>
                  </button>

                  <div class="flex shrink-0 gap-2">
                    <UButton size="xs" color="neutral" variant="ghost" @click="editActivity(activity.id)">
                      Edit
                    </UButton>
                    <UButton
                      size="xs" color="error" variant="ghost" :disabled="isSaving"
                      @click="removeActivity(activity.id)"
                    >
                      Remove
                    </UButton>
                  </div>
                </div>

                <div class="mt-3 grid gap-2 sm:grid-cols-2">
                  <div class="rounded-md bg-muted px-3 py-2 text-sm text-toned">
                    Personnel: {{ activity.personnel || 'None listed' }}
                  </div>
                  <div class="rounded-md bg-muted px-3 py-2 text-sm text-toned">
                    Location: {{ activity.location || 'None listed' }}
                  </div>
                </div>
              </div>
            </div>

            <div v-else class="rounded-lg border border-dashed border-default px-4 py-8 text-center text-sm text-muted">
              No activities yet. Start by adding one.
            </div>
          </div>
        </UCard>

        <div class="space-y-6">
          <UCard>
            <div class="space-y-5">
              <div class="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                <div>
                  <h2 class="text-lg font-semibold text-highlighted">
                    {{ editorPanel === 'form' ? activeFormTitle : 'Schedule JSON' }}
                  </h2>
                  <p class="text-sm text-muted">
                    {{ editorPanel === 'form' ? 'Update one activity at a time.' : 'Bulk edit the full day schedule.' }}
                  </p>
                </div>

                <div class="flex gap-2">
                  <UButton
                    size="sm" color="neutral" :variant="editorPanel === 'form' ? 'solid' : 'ghost'"
                    @click="editorPanel = 'form'"
                  >
                    Form
                  </UButton>
                  <UButton
                    size="sm" color="neutral" :variant="editorPanel === 'json' ? 'solid' : 'ghost'"
                    @click="openJsonEditor"
                  >
                    JSON
                  </UButton>
                </div>
              </div>

              <template v-if="editorPanel === 'form'">
                <form class="space-y-4" @submit.prevent="submitForm">
                  <UFormField label="Title" required>
                    <UInput v-model="form.title" class="w-full" @update:model-value="markFormDirty" />
                  </UFormField>

                  <div class="grid gap-4 sm:grid-cols-2">
                    <UFormField label="Start" required>
                      <UInput v-model="form.start" type="time" class="w-full" @update:model-value="markFormDirty" />
                    </UFormField>

                    <UFormField label="End" required>
                      <UInput v-model="form.end" type="time" class="w-full" @update:model-value="markFormDirty" />
                    </UFormField>
                  </div>

                  <UFormField label="Personnel">
                    <UInput v-model="form.personnel" class="w-full" @update:model-value="markFormDirty" />
                  </UFormField>

                  <UFormField label="Location">
                    <UInput v-model="form.location" class="w-full" @update:model-value="markFormDirty" />
                  </UFormField>

                  <div class="flex flex-wrap items-center justify-between gap-3 pt-2">
                    <UButton
                      type="button" color="neutral" variant="subtle" :disabled="isSaving"
                      @click="prepareNewActivityForm"
                    >
                      Clear
                    </UButton>

                    <div class="flex flex-wrap gap-3">
                      <UButton type="button" color="neutral" variant="subtle" @click="openJsonEditor">
                        Edit JSON instead
                      </UButton>
                      <UButton type="submit" :disabled="isSaving">
                        {{ saveButtonLabel }}
                      </UButton>
                    </div>
                  </div>
                </form>
              </template>

              <template v-else>
                <UFormField
                  label="Schedule JSON"
                  description="Each activity should include title, start, end, personnel and location. Times must use HH:mm."
                >
                  <UTextarea
                    :model-value="jsonEditorText" :rows="18" class="w-full font-mono text-sm"
                    @update:model-value="onJsonEditorUpdate"
                  />
                </UFormField>

                <div
                  v-if="jsonEditorError"
                  class="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm font-medium text-red-700 dark:border-red-900/60 dark:bg-red-950/40 dark:text-red-300"
                >
                  {{ jsonEditorError }}
                </div>

                <div class="flex flex-wrap items-center justify-between gap-3">
                  <UButton type="button" color="neutral" variant="subtle" :disabled="isSaving" @click="resetJsonEditor">
                    Reset JSON
                  </UButton>

                  <div class="flex flex-wrap gap-3">
                    <UButton type="button" color="neutral" variant="subtle" @click="editorPanel = 'form'">
                      Back to form
                    </UButton>
                    <UButton type="button" :disabled="isSaving" @click="applyJsonEditorChanges">
                      Apply JSON
                    </UButton>
                  </div>
                </div>
              </template>
            </div>
          </UCard>

          <UCard>
            <div class="space-y-4">
              <div class="flex items-center justify-between gap-3">
                <div>
                  <h2 class="text-lg font-semibold text-highlighted">
                    Live summary
                  </h2>
                  <p class="text-sm text-muted">
                    Current screen state at a glance.
                  </p>
                </div>

                <UBadge color="neutral" variant="soft">
                  {{ clockTimeText }}
                </UBadge>
              </div>

              <div
                v-if="seatingAlertMessage"
                class="rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm font-medium text-red-700 dark:border-red-900/60 dark:bg-red-950/40 dark:text-red-300"
              >
                {{ seatingAlertMessage }}
              </div>

              <div class="grid gap-4">
                <div class="rounded-lg border border-default p-4">
                  <div class="mb-3 flex flex-wrap items-center gap-2">
                    <UBadge :color="showUpcomingAsPrimary ? 'neutral' : 'warning'" variant="soft">
                      {{ primaryLabel }}
                    </UBadge>
                    <UBadge v-if="primaryActivity && renderedCurrent" color="primary" variant="subtle">
                      {{ primaryCountdownText }}
                    </UBadge>
                  </div>

                  <p class="text-2xl font-semibold tracking-tight text-highlighted">
                    {{ primaryActivity?.title || (renderedNext ? 'No activity is in progress right now.' : 'There are no more scheduled activities for today.') }}
                  </p>

                  <div v-if="primaryActivity" class="mt-4 grid gap-3 sm:grid-cols-2">
                    <div
                      v-for="item in primaryMetaItems" :key="`preview-${item.key}`"
                      class="rounded-md bg-muted px-3 py-2"
                    >
                      <div class="text-xs font-semibold uppercase tracking-wide text-muted">
                        {{ item.key }}
                      </div>
                      <div class="mt-1 text-sm font-medium text-highlighted">
                        {{ item.value }}
                      </div>
                    </div>
                  </div>
                </div>

                <div class="rounded-lg border border-default p-4">
                  <div class="mb-3 flex items-center gap-2">
                    <UBadge color="neutral" variant="soft">
                      Next activity
                    </UBadge>
                  </div>

                  <p class="text-lg font-semibold text-highlighted">
                    {{ renderedNext?.title || 'No upcoming activity' }}
                  </p>

                  <div v-if="renderedNext" class="mt-3 grid gap-3 sm:grid-cols-2">
                    <div
                      v-for="item in getMetaItems(renderedNext)" :key="`next-preview-${item.key}`"
                      class="rounded-md bg-muted px-3 py-2"
                    >
                      <div class="text-xs font-semibold uppercase tracking-wide text-muted">
                        {{ item.key }}
                      </div>
                      <div class="mt-1 text-sm font-medium text-highlighted">
                        {{ item.value }}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </UCard>

          <UCard>
            <div class="space-y-4">
              <div>
                <h2 class="text-lg font-semibold text-highlighted">
                  Later today
                </h2>
                <p class="text-sm text-muted">
                  Activities queued after the next item.
                </p>
              </div>

              <div v-if="renderedLaterActivities.length" class="space-y-3">
                <div
                  v-for="activity in renderedLaterActivities" :key="`later-${activity.id}`"
                  class="flex items-start justify-between gap-4 rounded-lg border border-default px-4 py-3"
                >
                  <div class="min-w-0">
                    <p class="truncate text-sm font-semibold text-highlighted">
                      {{ activity.title }}
                    </p>
                    <p class="truncate text-xs text-muted">
                      {{ activity.personnel || 'No personnel listed' }}
                    </p>
                  </div>
                  <div class="text-sm font-semibold tabular-nums text-toned">
                    {{ activity.start }}
                  </div>
                </div>
              </div>

              <div v-else class="rounded-lg border border-dashed border-default px-4 py-6 text-sm text-muted">
                Nothing else is queued after the next activity.
              </div>
            </div>
          </UCard>
        </div>
      </div>
    </div>
  </main>
</template>
