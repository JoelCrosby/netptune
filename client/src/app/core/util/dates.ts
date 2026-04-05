import dayjs from 'dayjs';
import RelativeTime from 'dayjs/plugin/relativeTime';
import UTC from 'dayjs/plugin/utc';
import LocalizedFormat from 'dayjs/plugin/localizedFormat';

export const fromNow = (value: string | Date | undefined | null): string => {
  if (!value) {
    return '';
  }

  dayjs.extend(RelativeTime);
  dayjs.extend(UTC);
  dayjs.extend(LocalizedFormat);

  return dayjs.utc(value).local().fromNow();
};
