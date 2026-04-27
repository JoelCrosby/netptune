export enum TaskPriority {
  none = 0,
  low = 1,
  medium = 2,
  high = 3,
  critical = 4,
}

export const taskPriorityLabels: Record<TaskPriority, string> = {
  [TaskPriority.none]: 'None',
  [TaskPriority.low]: 'Low',
  [TaskPriority.medium]: 'Medium',
  [TaskPriority.high]: 'High',
  [TaskPriority.critical]: 'Critical',
};

export const taskPriorityColors: Record<TaskPriority, string> = {
  [TaskPriority.none]: 'text-zinc-400',
  [TaskPriority.low]: 'text-blue-400',
  [TaskPriority.medium]: 'text-yellow-400',
  [TaskPriority.high]: 'text-orange-400',
  [TaskPriority.critical]: 'text-red-500',
};

export const taskPriorityIconColors: Record<TaskPriority, string> = {
  [TaskPriority.none]: 'stroke-zinc-400',
  [TaskPriority.low]: 'stroke-blue-400',
  [TaskPriority.medium]: 'stroke-yellow-400',
  [TaskPriority.high]: 'stroke-orange-400',
  [TaskPriority.critical]: 'stroke-red-500',
};

export const taskPriorityOptions = Object.values(TaskPriority)
  .filter((v): v is TaskPriority => typeof v === 'number')
  .map((value) => ({ value, label: taskPriorityLabels[value] }));
