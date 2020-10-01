import { selectTasksFeature } from '@core/core.state';
import { createSelector } from '@ngrx/store';
import { adapter, TasksState } from './tasks.model';

const { selectAll } = adapter.getSelectors();

export const selectTasks = createSelector(selectTasksFeature, selectAll);

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
