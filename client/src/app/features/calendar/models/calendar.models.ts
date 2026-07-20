import { ScheduledTask } from '@core/models/scheduled-task';

export interface CalendarViewModel {
  from: string;
  to: string;
  tasks: ScheduledTask[];
  truncated: boolean;
}

export interface CalendarDay {
  date: string;
  dayNumber: number;
  currentMonth: boolean;
  today: boolean;
}

export interface CalendarTaskMove {
  task: ScheduledTask;
  fromDate: string;
  toDate: string;
}

export const calendarTaskDragType = 'application/x-netptune-calendar-task';
