export enum TaskStatus {
  new = 0,
  complete = 1,
  inProgress = 2,
  onHold = 3,
  unAssigned = 4,
  awaitingClassification = 5,
  inActive = 6,
}

export const taskStatusLabels: Record<TaskStatus, string> = {
  [TaskStatus.new]: 'New',
  [TaskStatus.complete]: 'Complete',
  [TaskStatus.inProgress]: 'In Progress',
  [TaskStatus.onHold]: 'On Hold',
  [TaskStatus.unAssigned]: 'Un-assigned',
  [TaskStatus.awaitingClassification]: 'Awaiting Classification',
  [TaskStatus.inActive]: 'Inactive',
};

export const taskStatusOptions = [
  TaskStatus.new,
  TaskStatus.inProgress,
  TaskStatus.onHold,
  TaskStatus.unAssigned,
  TaskStatus.awaitingClassification,
  TaskStatus.inActive,
  TaskStatus.complete,
];
