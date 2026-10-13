import { SprintStatus } from '@core/enums/sprint-status';
import { SprintBurndownReport } from '@core/models/reporting';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';

export type SprintPace = 'ahead' | 'onTrack' | 'behind';

export interface SprintHealth {
  progressPercent: number;
  timePercent: number | null;
  dayNumber: number;
  dayCount: number;
  pace: SprintPace | null;
  velocity: number | null;
  requiredVelocity: number | null;
  projectedFinish: Date | null;
  daysEarly: number | null;
}

const msPerDay = 86_400_000;
const aheadMargin = 5;
const behindMargin = 10;

export function startOfDay(value: string | Date): Date {
  const date = new Date(value);
  date.setHours(0, 0, 0, 0);

  return date;
}

export function addDays(date: Date, days: number): Date {
  return new Date(date.getTime() + days * msPerDay);
}

export function daysBetween(from: Date, to: Date): number {
  return Math.round((to.getTime() - from.getTime()) / msPerDay);
}

export function sprintHealth(
  sprint: SprintDetailViewModel,
  burndown: SprintBurndownReport | undefined,
  now = new Date()
): SprintHealth {
  const today = startOfDay(now);
  const start = startOfDay(sprint.startedAt ?? sprint.startDate);
  const end = startOfDay(sprint.endDate);
  const duration = Math.max(1, daysBetween(start, end));
  const elapsed = Math.min(duration, Math.max(0, daysBetween(start, today)));
  const daysLeft = Math.max(0, daysBetween(today, end));

  const progressPercent = sprint.taskCount
    ? Math.round((sprint.doneTaskCount / sprint.taskCount) * 100)
    : 0;

  const health: SprintHealth = {
    progressPercent,
    timePercent: null,
    dayNumber: elapsed + 1,
    dayCount: duration + 1,
    pace: null,
    velocity: null,
    requiredVelocity: null,
    projectedFinish: null,
    daysEarly: null,
  };

  if (sprint.status !== SprintStatus.active) return health;

  const timePercent = Math.round((elapsed / duration) * 100);
  const { completed, remaining } = workCounts(sprint, burndown);
  const velocity = completed / Math.max(1, elapsed);
  const projectedFinish = projectFinish(today, remaining, velocity);

  return {
    ...health,
    timePercent,
    pace: paceFor(progressPercent, timePercent),
    velocity,
    requiredVelocity: remaining / Math.max(1, daysLeft),
    projectedFinish,
    daysEarly: projectedFinish ? daysBetween(projectedFinish, end) : null,
  };
}

// The burndown only counts work finished while the sprint ran, so it is the better
// measure of pace when the viewer can see it.
function workCounts(
  sprint: SprintDetailViewModel,
  burndown: SprintBurndownReport | undefined
): { completed: number; remaining: number } {
  const latest = burndown?.points.at(-1);

  if (latest) {
    return {
      completed: latest.totalScope - latest.remaining,
      remaining: latest.remaining,
    };
  }

  return {
    completed: sprint.doneTaskCount,
    remaining: sprint.taskCount - sprint.doneTaskCount,
  };
}

function projectFinish(
  today: Date,
  remaining: number,
  velocity: number
): Date | null {
  if (remaining <= 0) return today;
  if (velocity <= 0) return null;

  return addDays(today, Math.ceil(remaining / velocity));
}

function paceFor(progressPercent: number, timePercent: number): SprintPace {
  const lead = progressPercent - timePercent;

  if (lead >= aheadMargin) return 'ahead';
  if (lead <= -behindMargin) return 'behind';

  return 'onTrack';
}
