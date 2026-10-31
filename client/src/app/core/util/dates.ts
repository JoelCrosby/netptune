import { formatDate } from '@angular/common';
import dayjs from 'dayjs';
import RelativeTime from 'dayjs/plugin/relativeTime';
import UTC from 'dayjs/plugin/utc';
import LocalizedFormat from 'dayjs/plugin/localizedFormat';
import 'dayjs/locale/en-gb';
import 'dayjs/locale/fr';
import 'dayjs/locale/de';
import 'dayjs/locale/es';
import { appLocale } from './locale';

dayjs.extend(RelativeTime);
dayjs.extend(UTC);
dayjs.extend(LocalizedFormat);

dayjs.locale(appLocale.toLowerCase());

let cachedHostTimeZone: string | undefined;

export const hostTimeZone = (): string => {
  cachedHostTimeZone ??=
    Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';

  return cachedHostTimeZone;
};

/** Calendar date in `YYYY-MM-DD` form, as the reporting endpoints expect. */
export const isoDateValue = (date: Date): string =>
  date.toISOString().slice(0, 10);

// Local calendar date in `YYYY-MM-DD` form, as `<input type="date">` expects.
export const toDateInputValue = (date: Date): string => {
  const year = date.getFullYear();
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');

  return `${year}-${month}-${day}`;
};

export const fromNow = (value: string | Date | undefined | null): string => {
  if (!value) {
    return '';
  }

  return dayjs.utc(value).local().fromNow();
};

export const prettyDate = (value: Date | undefined | null): string => {
  if (!value) {
    return '';
  }

  return dayjs.utc(value).local().format('llll');
};

export const prettyShortDate = (
  value: string | Date | undefined | null
): string => {
  if (!value) {
    return '';
  }

  return dayjs.utc(value).local().format('ddd, D MMM LT');
};

export const timeRange = (
  first: string | Date,
  last: string | Date,
  locale = appLocale
): string => {
  const end = formatDate(last, 'medium', locale);
  const isInstant = dayjs(first).isSame(last);

  if (isInstant) return end;

  return `${formatDate(first, 'medium', locale)} – ${end}`;
};

export const localDayKey = (value: string | Date): string => {
  return dayjs.utc(value).local().format('YYYY-MM-DD');
};

export const dayHeading = (value: string | Date, now = new Date()): string => {
  const day = dayjs.utc(value).local().startOf('day');
  const today = dayjs(now).startOf('day');
  const daysAgo = today.diff(day, 'day');

  if (daysAgo === 0) {
    return $localize`:Heading over the entries from today in a timeline:Today`;
  }

  if (daysAgo === 1) {
    return $localize`:Heading over the entries from yesterday in a timeline:Yesterday`;
  }

  const format = day.year() === today.year() ? 'dddd D MMMM' : 'D MMMM YYYY';

  return day.format(format);
};

export const compactTime = (value: string | Date, now = new Date()): string => {
  const time = dayjs.utc(value).local();
  const isToday = time.isSame(now, 'day');

  if (!isToday) return time.format('LT');

  const minutes = dayjs(now).diff(time, 'minute');

  if (minutes < 1) {
    return $localize`:Compact age of a timeline entry that happened less than a minute ago:just now`;
  }

  if (minutes < 60) {
    return $localize`:Compact age of a timeline entry in minutes, as in 5m. MINUTES is a number:${minutes}:MINUTES:m`;
  }

  const hours = Math.floor(minutes / 60);

  return $localize`:Compact age of a timeline entry in hours, as in 3h. HOURS is a number:${hours}:HOURS:h`;
};
