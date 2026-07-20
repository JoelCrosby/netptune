import { TimelineRange, TimelineRangeLayout } from './timeline.models';

export const timelineHeaderMinimumHeight = 80;
export const timelineRangeTop = 56;
export const timelineRangeLaneHeight = 24;

export const layoutTimelineRanges = (
  ranges: readonly TimelineRange[]
): TimelineRangeLayout[] => {
  const laneEnds: string[] = [];

  return [...ranges]
    .sort(
      (left, right) =>
        left.startDate.localeCompare(right.startDate) ||
        left.endDate.localeCompare(right.endDate) ||
        String(left.id).localeCompare(String(right.id))
    )
    .map((range) => {
      const startDate = range.startDate.slice(0, 10);
      const endDate = range.endDate.slice(0, 10);
      const availableLane = laneEnds.findIndex(
        (laneEnd) => startDate > laneEnd
      );
      const lane = availableLane === -1 ? laneEnds.length : availableLane;

      laneEnds[lane] = endDate;

      return { ...range, lane };
    });
};

export const timelineHeaderHeight = (
  ranges: readonly TimelineRange[]
): number => {
  const layouts = layoutTimelineRanges(ranges);
  const laneCount = layouts.reduce(
    (count, range) => Math.max(count, range.lane + 1),
    0
  );

  return Math.max(
    timelineHeaderMinimumHeight,
    timelineRangeTop + laneCount * timelineRangeLaneHeight
  );
};
