import dayjs from 'dayjs';
import RelativeTime from 'dayjs/plugin/relativeTime';
import UTC from 'dayjs/plugin/utc';
import LocalizedFormat from 'dayjs/plugin/localizedFormat';

let cachedHostTimeZone: string | undefined;

export const hostTimeZone = (): string => {
  cachedHostTimeZone ??=
    Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';

  return cachedHostTimeZone;
};

/** Calendar date in `YYYY-MM-DD` form, as the reporting endpoints expect. */
export const isoDateValue = (date: Date): string =>
  date.toISOString().slice(0, 10);

export const fromNow = (value: string | Date | undefined | null): string => {
  if (!value) {
    return '';
  }

  dayjs.extend(RelativeTime);
  dayjs.extend(UTC);
  dayjs.extend(LocalizedFormat);

  return dayjs.utc(value).local().fromNow();
};
