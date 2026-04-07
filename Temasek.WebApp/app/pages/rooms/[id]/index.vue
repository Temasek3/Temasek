<script setup lang="ts">
import type { RoomSignboardSnapshot, SignboardActivity } from './signboard-helpers'
import type { TemasekWebApiFeaturesRoomsSignboardContractsRoomSignboardResponse as RoomSignboardSnapshotContract } from '~/kubb'

import { useTemasekWebApiFeaturesRoomsSignboardGetEndpoint } from '~/kubb'
import {
  formatEndsIn,
  getActivitiesAfterNext,
  getMetaItems,
  getScheduleState,
  getSeatingAlertMessage,
  normalizeSchedule,
  sortSchedule,
} from './signboard-helpers'

definePageMeta({
  layout: 'rooms',
})

const route = useRoute()
const runtimeConfig = useRuntimeConfig()

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

const now = ref(new Date())
const renderedNow = ref(new Date())
const renderedCurrent = ref<SignboardActivity | null>(null)
const renderedNext = ref<SignboardActivity | null>(null)
const renderedLaterActivities = ref<SignboardActivity[]>([])
const seatingAlertMessage = ref('')

const clockTime = ref<HTMLElement | null>(null)
const currentTitle = ref<HTMLElement | null>(null)
const nextTitle = ref<HTMLElement | null>(null)

let tickIntervalId: number | null = null
let resizeHandler: (() => void) | null = null

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

const schedule = computed(() => normalizeSchedule(roomState.value?.schedule))
const sortedSchedule = computed(() => sortSchedule(schedule.value))

const clockTimeText = computed(() => (
  now.value.toLocaleTimeString([], {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  })
))

const clockDateText = computed(() => (
  now.value.toLocaleDateString([], {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  })
))

const currentEmptyMessage = computed(() => (
  renderedNext.value
    ? 'No activity is in progress right now.'
    : 'There are no more scheduled activities for today.'
))

const showUpcomingAsPrimary = computed(() => !renderedCurrent.value && !!renderedNext.value)
const primaryActivity = computed(() => renderedCurrent.value || renderedNext.value || null)
const primaryLabel = computed(() => showUpcomingAsPrimary.value ? 'Next Activity' : 'Current Activity')
const primaryCardClass = computed(() => showUpcomingAsPrimary.value ? 'upcoming-main' : 'current')
const primaryHasCountdown = computed(() => Boolean(renderedCurrent.value))
const primaryCountdownText = computed(() => formatEndsIn(primaryActivity.value, renderedNow.value))
const primaryMetaItems = computed(() => primaryActivity.value ? getMetaItems(primaryActivity.value) : [])
const nextMetaItems = computed(() => renderedNext.value ? getMetaItems(renderedNext.value) : [])
const showNextPanel = computed(() => Boolean(renderedCurrent.value))
const shouldTriggerPixelPal = computed(() => {
  const queryValue = route.query.pixelPal
  const normalized = Array.isArray(queryValue) ? queryValue[0] : queryValue

  return normalized === '1' || normalized === 'true' || normalized === 'yes'
})

function getSignboardPath(targetRoomId: string = roomId.value) {
  return `/rooms/${encodeURIComponent(targetRoomId)}/signboard`
}

function buildApiUrl(path: string) {
  return apiBaseUrl.value ? new URL(path, apiBaseUrl.value).toString() : path
}

function applyRoomSnapshot(snapshot: Partial<RoomSignboardSnapshotContract> | null | undefined, targetRoomId: string = roomId.value) {
  roomState.value = {
    roomId: String(snapshot?.roomId ?? targetRoomId),
    name: String(snapshot?.name ?? targetRoomId),
    schedule: normalizeSchedule(snapshot?.schedule),
    updatedAtUtc: snapshot?.updatedAtUtc ? String(snapshot.updatedAtUtc) : undefined,
  }
}

function fitTitleElement(container: HTMLElement | null, maxRem: number, minRem: number) {
  if (!container) {
    return
  }

  const rootFontSize = Number.parseFloat(window.getComputedStyle(document.documentElement).fontSize)
  const maxPx = maxRem * rootFontSize
  const minPx = minRem * rootFontSize

  container.style.fontSize = `${maxPx}px`

  while (container.scrollHeight > container.clientHeight && Number.parseFloat(container.style.fontSize) > minPx) {
    container.style.fontSize = `${Number.parseFloat(container.style.fontSize) - 2}px`
  }
}

function fitSingleLineElement(container: HTMLElement | null, maxRem: number, minRem: number) {
  if (!container || container.clientWidth <= 0) {
    return
  }

  const rootFontSize = Number.parseFloat(window.getComputedStyle(document.documentElement).fontSize)
  const maxPx = maxRem * rootFontSize
  const minPx = minRem * rootFontSize
  const target = (container.firstElementChild as HTMLElement | null) ?? container

  container.style.fontSize = `${maxPx}px`

  while (
    (target.scrollWidth > container.clientWidth || target.scrollHeight > container.clientHeight)
    && Number.parseFloat(container.style.fontSize) > minPx
  ) {
    container.style.fontSize = `${Number.parseFloat(container.style.fontSize) - 1}px`
  }
}

function fitActivityTitles() {
  fitTitleElement(currentTitle.value, 14, 4.5)
  fitTitleElement(nextTitle.value, 7, 3)
}

function fitClockTime() {
  fitSingleLineElement(clockTime.value, 10, 4)
}

function fitMetaValues() {
  document.querySelectorAll<HTMLElement>('.meta-value').forEach((container) => {
    const isPrimary = Boolean(container.closest('.activity-card.current, .activity-card.upcoming-main'))
    fitSingleLineElement(container, isPrimary ? 4 : 3.25, isPrimary ? 1.75 : 1.5)
  })
}

function renderActivityState(currentTime: Date) {
  const state = getScheduleState(sortedSchedule.value, currentTime)

  renderedNow.value = new Date(currentTime.getTime())
  renderedCurrent.value = state.current ? { ...state.current } : null
  renderedNext.value = state.next ? { ...state.next } : null
  renderedLaterActivities.value = getActivitiesAfterNext(sortedSchedule.value, state.next).map(activity => ({ ...activity }))

  nextTick(() => {
    fitClockTime()
    fitActivityTitles()
    fitMetaValues()
  })
}

function refreshDashboard(currentTime: Date = now.value) {
  const state = getScheduleState(sortedSchedule.value, currentTime)
  seatingAlertMessage.value = getSeatingAlertMessage(state.next, currentTime)
  renderActivityState(currentTime)
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

watch(roomId, (nextRoomId) => {
  applyRoomSnapshot(null, nextRoomId)
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

watch(schedule, () => {
  refreshDashboard(now.value)
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
  refreshDashboard(now.value)

  resizeHandler = () => {
    fitClockTime()
    fitActivityTitles()
    fitMetaValues()
  }

  window.addEventListener('resize', resizeHandler)

  tickIntervalId = window.setInterval(() => {
    now.value = new Date()
    refreshDashboard(now.value)
  }, 1000)
})

onBeforeUnmount(() => {
  closeRoomStream()

  if (tickIntervalId !== null) {
    window.clearInterval(tickIntervalId)
  }

  if (resizeHandler) {
    window.removeEventListener('resize', resizeHandler)
  }
})
</script>

<template>
  <main
    class="dashboard flex min-h-screen flex-col gap-3 bg-linear-to-b from-slate-100 via-slate-50 to-slate-200 p-4 text-slate-900 antialiased [text-rendering:optimizeLegibility] dark:from-slate-900 dark:via-slate-950 dark:to-slate-950 dark:text-slate-50 lg:gap-4 lg:p-5 xl:p-6"
  >
    <PixelPalEasterEgg :trigger="shouldTriggerPixelPal" />

    <section class="grid gap-3 lg:grid-cols-[auto_minmax(0,1fr)] lg:items-start lg:gap-4">
      <div
        class="flex w-full flex-col items-start justify-center gap-3 rounded-2xl border border-slate-300 bg-linear-to-b from-white to-slate-50 p-5 shadow-lg shadow-slate-900/10 dark:border-slate-700 dark:from-slate-900 dark:to-slate-950 dark:shadow-black/30 lg:w-auto lg:self-start lg:p-6"
      >
        <div
          ref="clockTime"
          class="w-full whitespace-nowrap text-9xl font-black leading-none tracking-tight text-sky-700 tabular-nums dark:text-sky-400 lg:w-auto"
        >
          {{ clockTimeText }}
        </div>
        <div class="text-xl font-semibold leading-tight text-slate-600 dark:text-slate-400 md:text-2xl xl:text-3xl">
          {{ clockDateText }}
        </div>
      </div>

      <aside
        v-if="renderedLaterActivities.length"
        class="grid h-fit w-full min-w-0 max-w-xl gap-3 overflow-hidden rounded-2xl border border-slate-200 bg-white/90 p-4 shadow-lg shadow-slate-900/10 backdrop-blur-sm dark:border-slate-700 dark:bg-slate-900/90 dark:shadow-black/30 lg:justify-self-end"
      >
        <div class="text-sm font-bold uppercase tracking-widest text-slate-600 dark:text-slate-400">
          Later Today
        </div>
        <div class="grid min-h-0 gap-2 overflow-auto pr-1">
          <div
            v-for="activity in renderedLaterActivities"
            :key="activity.id"
            class="flex items-start gap-2"
          >
            <div class="w-20 shrink-0 text-lg font-bold leading-tight text-slate-500 tabular-nums dark:text-slate-300">
              {{ activity.start }}
            </div>
            <div class="flex-1 truncate text-xl font-semibold leading-tight text-slate-700 dark:text-slate-200">
              {{ activity.title }}
            </div>
          </div>
        </div>
      </aside>
    </section>

    <section
      v-if="seatingAlertMessage"
      class="overflow-hidden rounded-3xl border-2 border-red-900/20 bg-linear-to-r from-red-600 via-red-700 to-red-800 shadow-xl shadow-red-950/25"
    >
      <div class="flex min-w-full w-max items-center gap-10 py-4 animate-[seating-marquee-scroll_18s_linear_infinite] will-change-transform">
        <div class="shrink-0 whitespace-nowrap pl-6 text-4xl font-black uppercase leading-none tracking-tight text-white md:text-5xl xl:text-6xl">
          {{ seatingAlertMessage }}
        </div>
        <div
          aria-hidden="true"
          class="shrink-0 whitespace-nowrap pl-6 text-4xl font-black uppercase leading-none tracking-tight text-white md:text-5xl xl:text-6xl"
        >
          {{ seatingAlertMessage }}
        </div>
      </div>
    </section>

    <section
      class="grid min-h-0 flex-1 gap-3 lg:gap-4"
      :class="showNextPanel ? 'grid-cols-1 lg:grid-cols-5' : 'grid-cols-1'"
    >
      <article
        class="activity-card flex min-h-0 flex-col gap-3 rounded-2xl p-5 lg:p-6"
        :class="[
          showNextPanel ? 'lg:col-span-3' : '',
          primaryCardClass,
          primaryCardClass === 'current'
            ? 'border-4 border-amber-300 bg-linear-to-b from-amber-50 to-amber-100 shadow-xl shadow-amber-900/10 dark:border-amber-500/50 dark:from-amber-950/60 dark:to-amber-900/40 dark:shadow-black/30'
            : 'border-2 border-slate-300 bg-linear-to-b from-slate-50 to-slate-100 shadow-lg shadow-slate-900/10 dark:border-slate-700 dark:from-slate-900 dark:to-slate-950 dark:shadow-black/30',
        ]"
      >
        <div class="flex items-center justify-start">
          <div
            :class="showUpcomingAsPrimary
              ? 'inline-flex rounded-full border border-slate-300/70 bg-slate-200/80 px-3 py-2 text-lg font-bold uppercase tracking-widest text-slate-600 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-300'
              : 'inline-flex rounded-full border border-amber-300/60 bg-amber-200/60 px-3 py-2 text-lg font-extrabold uppercase tracking-widest text-amber-800 dark:border-amber-500/30 dark:bg-amber-500/15 dark:text-amber-300'"
          >
            {{ primaryLabel }}
          </div>
        </div>

        <h2
          ref="currentTitle"
          class="m-0 flex flex-1 items-center text-balance font-black leading-none tracking-tight"
          :class="primaryCardClass === 'current'
            ? 'text-7xl text-amber-950 dark:text-amber-100 md:text-8xl 2xl:text-9xl'
            : 'text-6xl text-slate-800 dark:text-slate-50 md:text-7xl xl:text-8xl'"
        >
          <template v-if="primaryActivity">
            {{ primaryActivity.title }}
          </template>
          <template v-else>
            No current activity
          </template>
        </h2>

        <div class="grid content-start gap-3 sm:grid-cols-2">
          <template v-if="primaryActivity">
            <div
              v-if="primaryHasCountdown"
              class="col-span-full inline-flex items-center justify-self-start rounded-full border border-slate-200 bg-white/70 px-4 py-2 text-3xl font-bold leading-tight tracking-tight text-sky-700 tabular-nums dark:border-slate-700 dark:bg-slate-900/60 dark:text-sky-300 xl:text-4xl"
            >
              {{ primaryCountdownText }}
            </div>

            <div
              v-for="item in primaryMetaItems"
              :key="`primary-${item.key}`"
              class="grid min-w-0 content-start gap-2 rounded-xl border border-slate-200/80 bg-white/80 p-5 dark:border-slate-700 dark:bg-slate-900/60"
            >
              <div class="text-base font-bold uppercase leading-tight tracking-widest text-slate-600 dark:text-slate-400 md:text-lg">
                {{ item.key }}
              </div>
              <div
                class="meta-value flex min-w-0 items-center overflow-hidden whitespace-nowrap text-5xl font-semibold leading-none tracking-tight text-slate-800 dark:text-slate-50 xl:text-6xl"
              >
                <span class="block min-w-0 w-full overflow-hidden text-ellipsis whitespace-nowrap">{{ item.value }}</span>
              </div>
            </div>
          </template>

          <template v-else>
            <div class="text-2xl font-medium leading-snug text-slate-600 dark:text-slate-400 md:text-3xl">
              {{ currentEmptyMessage }}
            </div>
          </template>
        </div>
      </article>

      <article
        v-if="showNextPanel"
        class="activity-card next flex min-h-0 flex-col gap-3 rounded-2xl border border-slate-300 bg-linear-to-b from-slate-50 to-slate-100 p-5 opacity-95 shadow-lg shadow-slate-900/10 dark:border-slate-700 dark:from-slate-900 dark:to-slate-950 dark:shadow-black/30 lg:col-span-2 lg:p-6"
      >
        <div class="flex items-center justify-start">
          <div class="inline-flex rounded-full border border-slate-300/70 bg-slate-200/80 px-3 py-1.5 text-base font-bold uppercase tracking-widest text-slate-600 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-300">
            Next Activity
          </div>
        </div>

        <h2
          ref="nextTitle"
          class="m-0 flex flex-1 items-center text-balance text-5xl font-black leading-none tracking-tight text-slate-800 dark:text-slate-50 md:text-6xl xl:text-7xl"
        >
          <template v-if="renderedNext">
            {{ renderedNext.title }}
          </template>
          <template v-else>
            No upcoming activity
          </template>
        </h2>

        <div class="grid content-start gap-3 sm:grid-cols-2">
          <template v-if="renderedNext">
            <div
              v-for="item in nextMetaItems"
              :key="`next-${item.key}`"
              class="grid min-w-0 content-start gap-2 rounded-xl border border-slate-200/80 bg-white/90 p-5 dark:border-slate-700 dark:bg-slate-900/70"
            >
              <div class="text-base font-bold uppercase leading-tight tracking-widest text-slate-600 dark:text-slate-400 md:text-lg">
                {{ item.key }}
              </div>
              <div class="meta-value flex min-w-0 items-center overflow-hidden whitespace-nowrap text-4xl font-semibold leading-none tracking-tight text-slate-800 dark:text-slate-50 xl:text-5xl">
                <span class="block min-w-0 w-full overflow-hidden text-ellipsis whitespace-nowrap">{{ item.value }}</span>
              </div>
            </div>
          </template>

          <template v-else>
            <div class="text-2xl font-medium leading-snug text-slate-600 dark:text-slate-400 md:text-3xl">
              No additional activities are scheduled after this.
            </div>
          </template>
        </div>
      </article>
    </section>
  </main>
</template>

<style scoped>
.dashboard {
  font-family: 'Atkinson Hyperlegible Next', 'Atkinson Hyperlegible', 'Trebuchet MS', 'Segoe UI', Arial, sans-serif;
}
</style>

<style>
@keyframes seating-marquee-scroll {
  from {
    transform: translateX(0);
  }

  to {
    transform: translateX(calc(-50% - 1.5rem));
  }
}
</style>
