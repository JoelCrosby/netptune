export type TimelineZoom = 'day' | 'week' | 'month';

export interface TimelineTick {
  date: string;
  label: string;
  left: number;
}

export interface TimelineRange {
  id: number | string;
  label: string;
  startDate: string;
  endDate: string;
}
