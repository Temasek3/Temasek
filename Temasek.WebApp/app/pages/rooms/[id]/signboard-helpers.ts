import { addMinutes, compareAsc, differenceInMinutes, format, isMatch, parse, startOfMinute } from 'date-fns'
import type {
  TemasekWebApiFeaturesRoomsSignboardContractsRoomSignboardResponse as RoomSignboardSnapshotContract,
  TemasekWebApiFeaturesRoomsSignboardContractsSignboardActivityDto as SignboardActivityContract,
} from '~/kubb'

type RequiredActivityKeys = 'id' | 'title' | 'start' | 'end' | 'personnel' | 'location'

export type SignboardActivity = Required<Pick<SignboardActivityContract, RequiredActivityKeys>>

export type RoomSignboardSnapshot = Omit<Required<RoomSignboardSnapshotContract>, 'schedule' | 'updatedAtUtc'> & {
  schedule: SignboardActivity[]
  updatedAtUtc?: string
}

export interface SignboardMetaItem {
  key: string
  value: string
}

const clockFormat = 'HH:mm'
const defaultClockTime = '00:00'
const referenceClockDate = new Date(2000, 0, 1, 0, 0, 0, 0)

function parseClockTime(value: string, referenceDate: Date = referenceClockDate) {
  const normalized = String(value ?? '').trim()
  if (!isMatch(normalized, clockFormat)) {
    return null
  }

  return parse(normalized, clockFormat, referenceDate)
}

function withReferenceDate(value: string, reference: Date) {
  return parseClockTime(value, startOfMinute(reference))
}

function formatDurationText(minutes: number) {
  const hours = Math.floor(minutes / 60)
  const remainingMinutes = minutes % 60

  if (hours > 0 && remainingMinutes > 0) {
    return `${hours}h ${remainingMinutes}m`
  }

  return hours > 0 ? `${hours}h` : `${remainingMinutes}m`
}

export function normalizeSchedule(value: unknown): SignboardActivity[] {
  if (!Array.isArray(value)) {
    return []
  }

  return value.map((activity, index) => normalizeActivity((activity ?? {}) as Partial<SignboardActivityContract>, index))
}

export function normalizeActivity(activity: Partial<SignboardActivityContract>, index: number): SignboardActivity {
  const start = String(activity.start ?? defaultClockTime).trim()
  const end = String(activity.end ?? defaultClockTime).trim()

  return {
    id: String(activity.id ?? `activity-${index + 1}`),
    title: String(activity.title ?? ''),
    start: isValidTimeText(start) ? start : defaultClockTime,
    end: isValidTimeText(end) ? end : defaultClockTime,
    personnel: String(activity.personnel ?? ''),
    location: String(activity.location ?? ''),
  }
}

export function sortSchedule(value: SignboardActivity[]) {
  return [...value].sort((left, right) => {
    const leftTime = parseClockTime(left.start) ?? referenceClockDate
    const rightTime = parseClockTime(right.start) ?? referenceClockDate
    return compareAsc(leftTime, rightTime)
  })
}

export function isValidTimeText(value: string) {
  return parseClockTime(value) !== null
}

export function hasPositiveDuration(start: string, end: string) {
  const startTime = parseClockTime(start)
  const endTime = parseClockTime(end)
  return Boolean(startTime && endTime && endTime.getTime() > startTime.getTime())
}

export function formatEndsIn(activity: SignboardActivity | null, currentTime: Date) {
  if (!activity) {
    return ''
  }

  const currentMinute = startOfMinute(currentTime)
  const endTime = withReferenceDate(activity.end, currentMinute)
  const diff = endTime ? differenceInMinutes(endTime, currentMinute) : 0

  return diff <= 0 ? 'Ending now' : `Ends in ${formatDurationText(diff)}`
}

export function getScheduleState(schedule: SignboardActivity[], currentTime: Date) {
  const currentMinute = startOfMinute(currentTime)
  let current: SignboardActivity | null = null
  let next: SignboardActivity | null = null

  for (const activity of schedule) {
    const startTime = withReferenceDate(activity.start, currentMinute)
    const endTime = withReferenceDate(activity.end, currentMinute)
    if (!startTime || !endTime) {
      continue
    }

    if (startTime <= currentMinute && currentMinute < endTime) {
      current = activity
      continue
    }

    if (!next && startTime > currentMinute) {
      next = activity
    }
  }

  return { current, next }
}

export function getActivitiesAfterNext(schedule: SignboardActivity[], nextActivity: SignboardActivity | null) {
  if (!nextActivity) {
    return []
  }

  const nextIndex = schedule.findIndex(activity => activity.id === nextActivity.id)
  return nextIndex === -1 ? [] : schedule.slice(nextIndex + 1)
}

export function getMetaItems(activity: SignboardActivity): SignboardMetaItem[] {
  return [
    { key: 'Start', value: activity.start },
    { key: 'End', value: activity.end },
    { key: 'Personnel', value: activity.personnel },
    { key: 'Location', value: activity.location },
  ]
}

export function getSeatingAlertMessage(nextActivity: SignboardActivity | null, currentTime: Date) {
  if (!nextActivity || !nextActivity.location.trim()) {
    return ''
  }

  const currentMinute = startOfMinute(currentTime)
  const startTime = withReferenceDate(nextActivity.start, currentMinute)
  if (!startTime) {
    return ''
  }

  const minutesUntilStart = differenceInMinutes(startTime, currentMinute)
  if (minutesUntilStart <= 0 || minutesUntilStart > 30) {
    return ''
  }

  const seatByTime = format(addMinutes(startTime, -5), clockFormat)
  return `Please be seated by ${seatByTime} for ${nextActivity.title}`
}

export function validateScheduleArray(value: unknown) {
  if (!Array.isArray(value)) {
    return { ok: false as const, message: 'JSON must be an array of activity objects.' }
  }

  const normalized = value.map((item, index) => normalizeActivity((item ?? {}) as Partial<SignboardActivityContract>, index))

  for (const activity of normalized) {
    if (!isValidTimeText(activity.start) || !isValidTimeText(activity.end)) {
      return { ok: false as const, message: 'Each activity must use HH:mm time strings for start and end.' }
    }

    if (!hasPositiveDuration(activity.start, activity.end)) {
      return { ok: false as const, message: 'Each activity must end after it starts.' }
    }
  }

  return { ok: true as const, normalized }
}

export function createActivityId() {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return `activity-${crypto.randomUUID()}`
  }

  return `activity-${Date.now()}`
}

export function getSuggestedStartTime(sourceSchedule: SignboardActivity[]) {
  if (sourceSchedule.length === 0) {
    return '09:00'
  }

  const latestActivity = sortSchedule(sourceSchedule)[sourceSchedule.length - 1]
  return latestActivity?.end || '09:00'
}

export function getSuggestedEndTime(startTime: string) {
  const parsedStart = parseClockTime(startTime) ?? parseClockTime(defaultClockTime)
  return parsedStart ? format(addMinutes(parsedStart, 60), clockFormat) : '01:00'
}
