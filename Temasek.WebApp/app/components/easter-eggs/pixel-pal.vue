<script setup lang="ts">
type PixelPalAction = 'wave' | 'hop' | 'spin' | 'moonwalk' | 'wobble'

interface Pixel {
  x: number
  y: number
  color: string
}

const props = withDefaults(defineProps<{
  enabled?: boolean
  trigger?: boolean
}>(), {
  enabled: true,
  trigger: false,
})

const SPRITE_ROWS = [
  '.......dddddddddd.......',
  '......ddggggggggdd......',
  '.....ddgggyygggggdd.....',
  '.....ddggggggggggdd.....',
  '....ddggffffffffggdd....',
  '....dggffkffffkffggd....',
  '.....dggffffffffggd.....',
  '.....ddggguuuuggddkbb...',
  '.....dgguuvvvvuggdkbbb..',
  '....ddgguvvyyvvuggdkbbb.',
  '....dggguvvbbvvuggdkbbb.',
  '....dgguuvvvvvuuggdkbb..',
  '.....ddgguuuuuuggddkb...',
  '......dguu....uudd......',
  '......dguu....uudd......',
  '......dgoo....oood......',
  '......oo......oo........',
  '......oo......oo........',
] as const

const PIXEL_SIZE = 5

const palette: Record<string, string> = {
  d: '#21301e',
  g: '#6c8f44',
  u: '#8fb15f',
  v: '#4f6f31',
  y: '#d6b94e',
  f: '#f4d0b1',
  k: '#151d16',
  b: '#60723c',
  o: '#2f261d',
}

const captions: Record<PixelPalAction, string> = {
  wave: 'steady lah!',
  hop: 'chiong ah!',
  spin: 'onz lor!',
  moonwalk: 'sabo me ah...',
  wobble: 'can one lah!',
}

const visible = ref(false)
const action = ref<PixelPalAction>('wave')
const position = reactive({ x: 24, y: 24 })
const spriteWidth = computed(() => Math.max(...SPRITE_ROWS.map(row => row.length)) * PIXEL_SIZE)
const spriteHeight = computed(() => SPRITE_ROWS.length * PIXEL_SIZE)
const shadowWidth = computed(() => Math.max(40, spriteWidth.value - 6))

const pixels = computed<Pixel[]>(() => {
  const rows = SPRITE_ROWS
  const mapped: Pixel[] = []

  rows.forEach((row, y) => {
    row.split('').forEach((code, x) => {
      const color = palette[code]
      if (color) {
        mapped.push({ x, y, color })
      }
    })
  })

  return mapped
})

const caption = computed(() => captions[action.value])

let showTimer: number | null = null
let hideTimer: number | null = null

function randomInt(min: number, max: number) {
  return Math.floor(Math.random() * (max - min + 1)) + min
}

function clearTimers() {
  if (showTimer !== null) {
    window.clearTimeout(showTimer)
    showTimer = null
  }

  if (hideTimer !== null) {
    window.clearTimeout(hideTimer)
    hideTimer = null
  }
}

function pickRandomAction(): PixelPalAction {
  const actions: PixelPalAction[] = ['wave', 'hop', 'spin', 'moonwalk', 'wobble']
  return actions[randomInt(0, actions.length - 1)] ?? 'wave'
}

function placeCharacter() {
  const viewportWidth = window.innerWidth
  const viewportHeight = window.innerHeight
  const margin = 20
  const estimatedWidth = spriteWidth.value + 44
  const estimatedHeight = spriteHeight.value + 72
  const sideLaneWidth = Math.max(96, Math.floor(viewportWidth * 0.14))
  const useLeftSide = Math.random() < 0.5

  if (useLeftSide) {
    position.x = randomInt(margin, Math.max(margin, sideLaneWidth - estimatedWidth))
  }
  else {
    const laneStart = Math.max(margin, viewportWidth - sideLaneWidth)
    position.x = randomInt(laneStart, Math.max(laneStart, viewportWidth - estimatedWidth - margin))
  }

  position.y = randomInt(100, Math.max(100, viewportHeight - estimatedHeight - margin))
}

function scheduleNextAppearance() {
  if (!props.enabled || !import.meta.client) {
    return
  }

  showTimer = window.setTimeout(() => {
    action.value = pickRandomAction()
    placeCharacter()
    visible.value = true

    hideTimer = window.setTimeout(() => {
      visible.value = false
      scheduleNextAppearance()
    }, randomInt(6500, 10500))
  }, randomInt(9000, 22000))
}

function triggerNow() {
  if (!props.enabled || !import.meta.client) {
    return
  }

  clearTimers()
  action.value = pickRandomAction()
  placeCharacter()
  visible.value = true

  hideTimer = window.setTimeout(() => {
    visible.value = false
    scheduleNextAppearance()
  }, randomInt(6000, 9000))
}

watch(() => props.enabled, (nextEnabled) => {
  clearTimers()
  visible.value = false

  if (nextEnabled && import.meta.client) {
    scheduleNextAppearance()
  }
}, { immediate: true })

watch(() => props.trigger, (next, prev) => {
  if (!import.meta.client) {
    return
  }

  if (next && !prev) {
    triggerNow()
  }
}, { immediate: true })

onBeforeUnmount(() => {
  clearTimers()
})
</script>

<template>
  <Transition name="pixel-pal-pop">
    <div
      v-if="visible"
      class="pixel-pal"
      :class="`is-${action}`"
      :style="{ left: `${position.x}px`, top: `${position.y}px` }"
      aria-hidden="true"
    >
      <div class="pixel-pal__bubble">
        {{ caption }}
      </div>

      <div
        class="pixel-pal__sprite"
        role="presentation"
        :style="{ width: `${spriteWidth}px`, height: `${spriteHeight}px` }"
      >
        <span
          v-for="pixel in pixels"
          :key="`${pixel.x}-${pixel.y}`"
          class="pixel-pal__pixel"
          :style="{
            left: `${pixel.x * PIXEL_SIZE}px`,
            top: `${pixel.y * PIXEL_SIZE}px`,
            width: `${PIXEL_SIZE}px`,
            height: `${PIXEL_SIZE}px`,
            backgroundColor: pixel.color,
          }"
        />
      </div>

      <div class="pixel-pal__shadow" :style="{ width: `${shadowWidth}px` }" />
    </div>
  </Transition>
</template>

<style scoped>
.pixel-pal {
  position: fixed;
  z-index: 45;
  pointer-events: none;
  transform-origin: center bottom;
}

.pixel-pal__bubble {
  width: fit-content;
  max-width: 9rem;
  margin: 0 auto 0.45rem;
  padding: 0.15rem 0.45rem;
  border: 1px solid rgba(47, 95, 57, 0.32);
  border-radius: 999px;
  background: rgba(239, 248, 233, 0.95);
  color: rgb(31, 41, 55);
  font-size: 0.7rem;
  font-weight: 700;
  line-height: 1.2;
  text-align: center;
  white-space: nowrap;
}

.pixel-pal__sprite {
  position: relative;
  margin: 0 auto;
  image-rendering: pixelated;
}

.pixel-pal__pixel {
  position: absolute;
}

.pixel-pal__shadow {
  height: 12px;
  margin: -2px auto 0;
  border-radius: 999px;
  background: color-mix(in srgb, var(--ui-text-muted, #334155) 28%, transparent);
  filter: blur(1px);
}

.is-wave {
  animation: pixel-pal-wave 1.6s steps(2) infinite;
}

.is-hop {
  animation: pixel-pal-hop 1.8s steps(2) infinite;
}

.is-spin {
  animation: pixel-pal-spin 2.4s linear infinite;
}

.is-moonwalk {
  animation: pixel-pal-moonwalk 2.1s steps(2) infinite;
}

.is-wobble {
  animation: pixel-pal-wobble 1.5s ease-in-out infinite;
}

.pixel-pal-pop-enter-active,
.pixel-pal-pop-leave-active {
  transition: opacity 220ms ease, transform 220ms ease;
}

.pixel-pal-pop-enter-from,
.pixel-pal-pop-leave-to {
  opacity: 0;
  transform: translateY(8px) scale(0.88);
}

@keyframes pixel-pal-wave {
  0%,
  100% {
    transform: rotate(0deg);
  }

  25% {
    transform: rotate(4deg);
  }

  75% {
    transform: rotate(-4deg);
  }
}

@keyframes pixel-pal-hop {
  0%,
  100% {
    transform: translateY(0);
  }

  50% {
    transform: translateY(-16px);
  }
}

@keyframes pixel-pal-spin {
  from {
    transform: rotate(0deg);
  }

  to {
    transform: rotate(360deg);
  }
}

@keyframes pixel-pal-moonwalk {
  0%,
  100% {
    transform: translateX(0) translateY(0) rotate(0deg);
  }

  20% {
    transform: translateX(-8px) translateY(-1px) rotate(-1deg);
  }

  45% {
    transform: translateX(-14px) translateY(0) rotate(1deg);
  }

  70% {
    transform: translateX(-6px) translateY(-1px) rotate(-1deg);
  }

  85% {
    transform: translateX(-2px) translateY(0) rotate(0deg);
  }
}

@keyframes pixel-pal-wobble {
  0%,
  100% {
    transform: rotate(0deg) scale(1);
  }

  35% {
    transform: rotate(-6deg) scale(1.03);
  }

  65% {
    transform: rotate(6deg) scale(0.98);
  }
}
</style>
