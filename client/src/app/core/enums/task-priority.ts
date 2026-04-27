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

export const taskPriorityCardColors: Record<TaskPriority, string> = {
  [TaskPriority.none]: '',
  [TaskPriority.low]: 'bg-blue-400/10! border-blue-900/20!',
  [TaskPriority.medium]: 'bg-yellow-400/10! border-yellow-900/20!',
  [TaskPriority.high]: 'bg-orange-400/10! border-orange-900/20!',
  [TaskPriority.critical]: 'bg-red-500/10! border-red-900/20!',
};

export const taskPriorityIconColors: Record<TaskPriority, string> = {
  [TaskPriority.none]: 'fill-zinc-400 text-zinc-400',
  [TaskPriority.low]: 'fill-blue-400 text-blue-400',
  [TaskPriority.medium]: 'fill-yellow-400 text-yellow-400',
  [TaskPriority.high]: 'fill-orange-400 text-orange-400',
  [TaskPriority.critical]: 'fill-red-500 text-red-500',
};

export const taskPriorityOptions = Object.values(TaskPriority)
  .filter((v): v is TaskPriority => typeof v === 'number')
  .map((value) => ({ value, label: taskPriorityLabels[value] }));
