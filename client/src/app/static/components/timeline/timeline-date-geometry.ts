import { TimelineZoom } from './timeline.models';

const millisecondsPerDay = 86_400_000;

export const timelineGridBackground = [
  'linear-gradient(to right, color-mix(in srgb, currentColor 24%, transparent) 1px, transparent 1px)',
  'linear-gradient(to right, color-mix(in srgb, currentColor 8%, transparent) 1px, transparent 1px)',
].join(', ');

export const timelineGridBackgroundSize = (
  dayWidth: number,
  majorIntervalDays: number
): string => `${dayWidth * majorIntervalDays}px 100%, ${dayWidth}px 100%`;

export const timelineDayWidth = (zoom: TimelineZoom): number => {
  switch (zoom) {
    case 'day':
      return 44;
    case 'week':
      return 24;
    case 'month':
      return 10;
  }
};

export const dateOffset = (from: string, date: string): number =>
  Math.round((dateValue(date) - dateValue(from)) / millisecondsPerDay);

export const inclusiveDayCount = (from: string, to: string): number =>
  dateOffset(from, to) + 1;

export const addDays = (date: string, days: number): string => {
  const value = new Date(dateValue(date) + days * millisecondsPerDay);
  return value.toISOString().slice(0, 10);
};

export const todayDate = (): string => new Date().toISOString().slice(0, 10);

export const dateLabel = (date: string, zoom: TimelineZoom): string => {
  const value = new Date(`${date}T00:00:00Z`);
  const options: Intl.DateTimeFormatOptions =
    zoom === 'month'
      ? { month: 'short', year: 'numeric', timeZone: 'UTC' }
      : { day: 'numeric', month: 'short', timeZone: 'UTC' };

  return new Intl.DateTimeFormat(undefined, options).format(value);
};

export const clippedRangeLeft = (
  from: string,
  start: string,
  dayWidth: number
): number => Math.max(0, dateOffset(from, start)) * dayWidth;

export const clippedRangeWidth = (
  from: string,
  to: string,
  start: string,
  end: string,
  dayWidth: number
): number => {
  const clippedStart = Math.max(0, dateOffset(from, start));
  const clippedEnd = Math.min(
    inclusiveDayCount(from, to) - 1,
    dateOffset(from, end)
  );

  return Math.max(dayWidth, (clippedEnd - clippedStart + 1) * dayWidth);
};

const dateValue = (date: string): number =>
  new Date(`${date}T00:00:00Z`).getTime();
