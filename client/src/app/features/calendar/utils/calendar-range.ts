import {
  addDays,
  todayDate,
} from '@static/components/timeline/timeline-date-geometry';
import { CalendarDay } from '../models/calendar.models';

const monthPattern = /^(\d{4})-(0[1-9]|1[0-2])$/;

export interface CalendarMonthRange {
  month: string;
  from: string;
  to: string;
  label: string;
  days: CalendarDay[];
}

export const validCalendarMonth = (value: string | null): string =>
  value && monthPattern.test(value) ? value : todayDate().slice(0, 7);

export const calendarMonthRange = (month: string): CalendarMonthRange => {
  const safeMonth = validCalendarMonth(month);
  const first = `${safeMonth}-01`;
  const weekday = new Date(`${first}T00:00:00Z`).getUTCDay();
  const from = addDays(first, -weekday);
  const to = addDays(from, 41);
  const today = todayDate();

  return {
    month: safeMonth,
    from,
    to,
    label: new Intl.DateTimeFormat(undefined, {
      month: 'long',
      year: 'numeric',
      timeZone: 'UTC',
    }).format(new Date(`${first}T00:00:00Z`)),
    days: Array.from({ length: 42 }, (_, index) => {
      const date = addDays(from, index);
      return {
        date,
        dayNumber: Number(date.slice(8, 10)),
        currentMonth: date.startsWith(safeMonth),
        today: date === today,
      };
    }),
  };
};

export const addCalendarMonths = (month: string, amount: number): string => {
  const [year, monthNumber] = validCalendarMonth(month).split('-').map(Number);
  const date = new Date(Date.UTC(year, monthNumber - 1 + amount, 1));
  return date.toISOString().slice(0, 7);
};

export const calendarDayLabel = (date: string): string =>
  new Intl.DateTimeFormat(undefined, {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
    timeZone: 'UTC',
  }).format(new Date(`${date}T00:00:00Z`));
