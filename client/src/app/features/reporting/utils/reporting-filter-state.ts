import { SprintStatus } from '@core/enums/sprint-status';
import { Sprint } from '@core/models/sprint';
import { ReportingGrouping } from '@core/models/reporting';

export const reportingDateValue = (date: Date): string =>
  date.toISOString().slice(0, 10);

export function defaultReportingRange(now = new Date()): {
  from: string;
  to: string;
} {
  return {
    from: reportingDateValue(
      new Date(now.getTime() - 90 * 24 * 60 * 60 * 1000)
    ),
    to: reportingDateValue(now),
  };
}

export function reportingGrouping(value: string | null): ReportingGrouping {
  return value === 'Week' ? 'Week' : 'Day';
}

export function defaultReportingSprintId(
  sprints: readonly Sprint[],
  projectId?: number
): number | undefined {
  const visibleSprints = sprints.filter(
    (sprint) => !projectId || sprint.projectId === projectId
  );
  const active = visibleSprints.find(
    (sprint) => sprint.status === SprintStatus.active
  );

  if (active) return active.id;

  return [...visibleSprints]
    .filter((sprint) => sprint.status === SprintStatus.completed)
    .sort((left, right) =>
      (right.completedAt ?? '').localeCompare(left.completedAt ?? '')
    )[0]?.id;
}
