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

export const prettyShortDate = (value: Date | undefined | null): string => {
  if (!value) {
    return '';
  }

  return dayjs.utc(value).local().format('ddd, D MMM LT');
};
