import { Params } from '@angular/router';
import { TaskPriority } from '@core/enums/task-priority';
import { StatusCategory } from '@core/models/status';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';

export type SprintTaskFilter =
  'all' | 'open' | 'highPriority' | 'unassigned' | 'noEstimate' | 'added';

export function isOpenTask(task: TaskViewModel): boolean {
  return task.statusCategory !== StatusCategory.done;
}

export function isHighPriority(task: TaskViewModel): boolean {
  return (
    task.priority === TaskPriority.high ||
    task.priority === TaskPriority.critical
  );
}

export function matchesSprintTaskFilter(
  task: TaskViewModel,
  filter: SprintTaskFilter,
  addedTaskIds: ReadonlySet<number>
): boolean {
  switch (filter) {
    case 'all':
      return true;
    case 'open':
      return isOpenTask(task);
    case 'highPriority':
      return isOpenTask(task) && isHighPriority(task);
    case 'unassigned':
      return isOpenTask(task) && !task.assignees.length;
    case 'noEstimate':
      return isOpenTask(task) && task.estimateValue == null;
    case 'added':
      return addedTaskIds.has(task.id);
  }
}

const openStatusCategories = [
  StatusCategory.new,
  StatusCategory.backlog,
  StatusCategory.todo,
  StatusCategory.active,
  StatusCategory.inactive,
];

// The api/tasks equivalent of matchesSprintTaskFilter, so the table can page and
// sort server-side while the attention counts come from the loaded sprint.
export function sprintTaskFilterParams(
  filter: SprintTaskFilter,
  addedTaskIds: ReadonlySet<number>
): Params {
  const open = { statusCategories: openStatusCategories };

  switch (filter) {
    case 'all':
      return {};
    case 'open':
      return open;
    case 'highPriority':
      return {
        ...open,
        priorities: [TaskPriority.high, TaskPriority.critical],
      };
    case 'unassigned':
      return { ...open, hasAssignee: false };
    case 'noEstimate':
      return { ...open, hasEstimate: false };
    case 'added':
      return { taskIds: [...addedTaskIds] };
  }
}
