import { createFeatureSelector, createSelector } from '@ngrx/store';
import { TasksState, adapter } from './tasks.model';
import { TaskStatus } from '@core/enums/project-task-status';
import { AppState } from '@app/core/core.state';

export interface State extends AppState {
  tasks: TasksState;
}

const selectTasksFeature = createFeatureSelector<State, TasksState>('tasks');

const { selectAll } = adapter.getSelectors();

export const selectTasks = createSelector(selectTasksFeature, selectAll);

export const selectTasksCompleted = createSelector(selectTasks, tasks =>
  tasks.filter(task => task.status === TaskStatus.Complete)
);

export const selectTasksOwner = createSelector(selectTasks, tasks =>
  tasks.filter(task => task.status === TaskStatus.New)
);

export const selectTasksBacklog = createSelector(selectTasks, tasks =>
  tasks.filter(task => task.status === TaskStatus.InActive)
);

export const selectTasksLoading = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.loading
);

export const selectTasksLoaded = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.loaded
);

export const selectSelectedTask = createSelector(
  selectTasksFeature,
  (state: TasksState) => state.selectedTask
);
