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

export const fromNow = (value: string | Date | undefined | null): string => {
  if (!value) {
    return '';
  }

  dayjs.extend(RelativeTime);
  dayjs.extend(UTC);
  dayjs.extend(LocalizedFormat);

  return dayjs.utc(value).local().fromNow();
};
