import { createSelector } from '@ngrx/store';
import { TasksState, adapter } from './tasks.model';
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
