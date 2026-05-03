import { selectTasksFeature } from '@core/core.state';
import { createSelector } from '@ngrx/store';
import { adapter, TasksState } from './tasks.model';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { netptunePermissions } from '@app/core/auth/permissions';
import { selectSelectedSprintFilterId } from '../sprints/sprints.selectors';

const { selectAll } = adapter.getSelectors();

export const selectAllTasks = createSelector(selectTasksFeature, selectAll);

export const selectTasks = createSelector(
  selectAllTasks,
  selectSelectedSprintFilterId,
  (tasks, sprintId) =>
    sprintId ? tasks.filter((task) => task.sprintId === sprintId) : tasks
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

export const selectDetailTaskIsRedOnly = createSelector(
  selectHasPermission(netptunePermissions.tasks.update),
  (state) => !state
);

export const selectRequiredDetailTask = createSelector(
  selectTasksFeature,
  (state: TasksState) => {
    if (!state.detailTask) {
      throw new Error('No task selected');
    }

    return state.detailTask;
  }
);

export const selectComments = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.comments
);
