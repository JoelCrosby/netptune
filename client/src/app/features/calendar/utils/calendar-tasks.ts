import { ScheduledTask, TaskSchedule } from '@core/models/scheduled-task';
import {
  addDays,
  dateOffset,
} from '@static/components/timeline/timeline-date-geometry';

export const taskStartsOn = (task: ScheduledTask): string | undefined =>
  task.startDate ?? task.dueDate ?? undefined;

export const taskEndsOn = (task: ScheduledTask): string | undefined =>
  task.dueDate ?? task.startDate ?? undefined;

export const taskOccursOn = (task: ScheduledTask, date: string): boolean => {
  const start = taskStartsOn(task);
  const end = taskEndsOn(task);
  return !!start && !!end && start <= date && end >= date;
};

export const moveTaskSchedule = (
  task: ScheduledTask,
  fromDate: string,
  toDate: string
): TaskSchedule => {
  const offset = dateOffset(fromDate, toDate);
  return {
    startDate: task.startDate ? addDays(task.startDate, offset) : null,
    endDate: task.dueDate ? addDays(task.dueDate, offset) : null,
  };
};

export const compareCalendarTasks = (
  left: ScheduledTask,
  right: ScheduledTask
): number => {
  const leftStart = taskStartsOn(left) ?? '';
  const rightStart = taskStartsOn(right) ?? '';
  const leftEnd = taskEndsOn(left) ?? leftStart;
  const rightEnd = taskEndsOn(right) ?? rightStart;

  return (
    leftStart.localeCompare(rightStart) ||
    rightEnd.localeCompare(leftEnd) ||
    left.projectName.localeCompare(right.projectName) ||
    left.id - right.id
  );
};

export type CalendarTaskLane = ScheduledTask | undefined;

export const calendarTaskLanesByDate = (
  tasks: ScheduledTask[],
  weeks: string[][]
): Map<string, CalendarTaskLane[]> => {
  const lanesByDate = new Map<string, CalendarTaskLane[]>();
  const sortedTasks = [...tasks].sort(compareCalendarTasks);

  for (const dates of weeks) {
    for (const date of dates) {
      lanesByDate.set(date, []);
    }

    const weekStart = dates.at(0);
    const weekEnd = dates.at(-1);
    if (!weekStart || !weekEnd) {
      continue;
    }

    const occupiedUntil: string[] = [];
    const weekTasks = sortedTasks.filter((task) => {
      const start = taskStartsOn(task);
      const end = taskEndsOn(task);
      return !!start && !!end && start <= weekEnd && end >= weekStart;
    });

    for (const task of weekTasks) {
      const taskStart = taskStartsOn(task);
      const taskEnd = taskEndsOn(task);
      if (!taskStart || !taskEnd) {
        continue;
      }

      const visibleStart = taskStart < weekStart ? weekStart : taskStart;
      const visibleEnd = taskEnd > weekEnd ? weekEnd : taskEnd;
      let laneIndex = occupiedUntil.findIndex(
        (occupiedEnd) => occupiedEnd < visibleStart
      );

      if (laneIndex === -1) {
        laneIndex = occupiedUntil.length;
      }
      occupiedUntil[laneIndex] = visibleEnd;

      for (const date of dates) {
        if (date < visibleStart || date > visibleEnd) {
          continue;
        }

        const lanes = lanesByDate.get(date);
        if (lanes) {
          lanes[laneIndex] = task;
        }
      }
    }
  }

  return lanesByDate;
};
