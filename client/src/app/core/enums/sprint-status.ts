export enum SprintStatus {
  planning = 0,
  active = 1,
  completed = 2,
  cancelled = 3,
}

export const sprintStatusLabels: Record<SprintStatus, string> = {
  [SprintStatus.planning]: 'Planning',
  [SprintStatus.active]: 'Active',
  [SprintStatus.completed]: 'Completed',
  [SprintStatus.cancelled]: 'Cancelled',
};
