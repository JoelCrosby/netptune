import { firstDayOfWeek, monthNames, weekdayNames } from '@core/util/locale';

export interface CalendarWeekday {
  narrow: string;
  long: string;
}

export interface CalendarMonth {
  index: number;
  long: string;
  short: string;
}

/**
 * Weekday column headers, rotated so index 0 is the locale's first day of the
 * week. en-GB, fr, de and es all start on Monday — only en-US starts on Sunday —
 * so a hardcoded Sunday-first list is wrong even for the source locale.
 *
 * Narrow names are single letters and ambiguous (M T W T F S S), so `long` is
 * carried alongside for assistive tech and hover.
 */
export const calendarWeekdays: CalendarWeekday[] = weekdayNames('narrow').map(
  (narrow, index) => ({ narrow, long: weekdayNames('long')[index] })
);

/** Month options for the month picker, in calendar order. */
export const calendarMonths: CalendarMonth[] = monthNames('long').map(
  (long, index) => ({ index, long, short: monthNames('short')[index] })
);

export interface CalendarDay {
  date: Date;
  value: string;
  currentMonth: boolean;
  today: boolean;
  selected: boolean;
  disabled: boolean;
}

export const parseDateValue = (
  value: string | null | undefined
): Date | null => {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value ?? '');

  if (!match) {
    return null;
  }

  const year = Number(match[1]);
  const month = Number(match[2]) - 1;
  const day = Number(match[3]);
  const date = makeDate(year, month, day);
  const valid =
    date.getFullYear() === year &&
    date.getMonth() === month &&
    date.getDate() === day;

  return valid ? date : null;
};

export const dateValue = (date: Date): string => {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
};

export const makeDate = (year: number, month: number, day: number): Date =>
  new Date(year, month, day, 12, 0, 0, 0);

export const addCalendarDays = (date: Date, days: number): Date =>
  makeDate(date.getFullYear(), date.getMonth(), date.getDate() + days);

export const addCalendarMonths = (date: Date, months: number): Date => {
  const target = makeDate(date.getFullYear(), date.getMonth() + months, 1);
  const lastDay = makeDate(
    target.getFullYear(),
    target.getMonth() + 1,
    0
  ).getDate();

  return makeDate(
    target.getFullYear(),
    target.getMonth(),
    Math.min(date.getDate(), lastDay)
  );
};

export const sameCalendarDay = (left: Date, right: Date): boolean =>
  dateValue(left) === dateValue(right);

export const startOfCalendarMonth = (date: Date): Date =>
  makeDate(date.getFullYear(), date.getMonth(), 1);

export const calendarDays = (
  viewDate: Date,
  selectedDate: Date | null,
  minDate: Date | null,
  maxDate: Date | null
): CalendarDay[] => {
  const first = startOfCalendarMonth(viewDate);
  // Back up to the locale's first day of the week, not unconditionally to Sunday.
  // getDay() is 0-based on Sunday, as is firstDayOfWeek().
  const leadingDays = (first.getDay() - firstDayOfWeek() + 7) % 7;
  const start = addCalendarDays(first, -leadingDays);
  const now = new Date();
  const today = makeDate(now.getFullYear(), now.getMonth(), now.getDate());

  return Array.from({ length: 42 }, (_, index) => {
    const date = addCalendarDays(start, index);

    return {
      date,
      value: dateValue(date),
      currentMonth: date.getMonth() === viewDate.getMonth(),
      today: sameCalendarDay(date, today),
      selected: selectedDate !== null && sameCalendarDay(date, selectedDate),
      disabled:
        (minDate !== null && date < minDate) ||
        (maxDate !== null && date > maxDate),
    };
  });
};
