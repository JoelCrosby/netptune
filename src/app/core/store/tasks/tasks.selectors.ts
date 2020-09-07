import { createSelector } from '@ngrx/store';
import { TasksState, adapter, TaskListGroup } from './tasks.model';
import { selectTasksFeature } from '@core/core.state';
import { TaskStatus } from '@core/enums/project-task-status';

const { selectAll } = adapter.getSelectors();

export const selectTasks = createSelector(selectTasksFeature, selectAll);

export const selectTasksCompleted = createSelector(selectTasks, (tasks) =>
  tasks
    .filter((task) => task.status === TaskStatus.Complete)
    .sort((a, b) => a.sortOrder - b.sortOrder)
);

export const selectTasksOwner = createSelector(selectTasks, (tasks) =>
  tasks
    .filter(
      (task) =>
        task.status === TaskStatus.New || task.status === TaskStatus.InProgress
    )
    .sort((a, b) => a.sortOrder - b.sortOrder)
);

export const selectTasksBacklog = createSelector(selectTasks, (tasks) =>
  tasks
    .filter(
      (task) =>
        task.status === TaskStatus.InActive ||
        task.status === TaskStatus.UnAssigned
    )
    .sort((a, b) => a.sortOrder - b.sortOrder)
);

export const selectTaskGroups = createSelector(
  selectTasksCompleted,
  selectTasksOwner,
  selectTasksBacklog,
  (completed, owner, backlog): TaskListGroup[] => [
    {
      groupName: 'my-tasks',
      tasks: owner,
      header: 'My Tasks',
      status: TaskStatus.New,
      emptyMessage: 'You have no tasks assigned to you',
    },
    {
      groupName: 'completed-tasks',
      tasks: completed,
      header: 'Completed Tasks',
      status: TaskStatus.Complete,
      emptyMessage: 'You currently have no completed tasks.',
    },
    {
      groupName: 'backlog-tasks',
      tasks: backlog,
      header: 'Backlog',
      status: TaskStatus.InActive,
      emptyMessage: 'Your backlog is currently empty hurray!',
    },
  ]
);

export const selectTasksLoading = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.loading && !state.loaded
);

export const selectTasksLoaded = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.loaded
);

export const selectSelectedTask = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.selectedTask
);

export const selectInlineEditActive = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.inlineEditActive
);

export const selectDetailTask = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.detailTask
);

export const selectComments = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.comments
);
