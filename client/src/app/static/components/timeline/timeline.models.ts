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

export interface TimelineRangeLayout extends TimelineRange {
  lane: number;
}

export interface TimelineHeaderGroup {
  id: string;
  label: string;
  left: number;
  width: number;
}

export interface TimelineDependency {
  id: number;
  sourceX: number;
  sourceY: number;
  targetX: number;
  targetY: number;
}

export interface TimelineSchedule {
  startDate: string | null;
  endDate: string | null;
}
