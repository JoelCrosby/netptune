import { numberFormat } from './locale';

const seconds = numberFormat({
  style: 'unit',
  unit: 'second',
  unitDisplay: 'narrow',
  maximumFractionDigits: 0,
});

const minutes = numberFormat({
  style: 'unit',
  unit: 'minute',
  unitDisplay: 'narrow',
  maximumFractionDigits: 0,
});

const millisecondsPerSecond = 1000;
const secondsPerMinute = 60;

/**
 * Elapsed time in its shortest readable form — "8s", or "2m 5s" past a minute —
 * for progress lines and durations sitting beside other text.
 */
export const formatElapsed = (durationMs: number): string => {
  const total = Math.max(0, Math.round(durationMs / millisecondsPerSecond));
  const isUnderAMinute = total < secondsPerMinute;

  if (isUnderAMinute) {
    return seconds.format(total);
  }

  const wholeMinutes = Math.floor(total / secondsPerMinute);
  const remainder = total % secondsPerMinute;

  return `${minutes.format(wholeMinutes)} ${seconds.format(remainder)}`;
};
