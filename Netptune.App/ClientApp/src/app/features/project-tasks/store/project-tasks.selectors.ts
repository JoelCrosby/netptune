import { createFeatureSelector, createSelector } from '@ngrx/store';
import { ProjectTasksState, selectAllProjectTasks } from './project-tasks.reducer';
import { ProjectTaskStatus } from '@app/core/enums/project-task-status';

export const selectProjectTasksFeature = createFeatureSelector<ProjectTasksState>('project-tasks');

export const selectTasksEntites = createSelector(
  selectProjectTasksFeature,
  (state: ProjectTasksState) => state.tasks
);

export const selectTasks = createSelector(
  selectTasksEntites,
  selectAllProjectTasks
);

export const selectTasksCompleted = createSelector(
  selectTasks,
  tasks => {
    if (tasks) {
      return tasks.filter(task => task.status === ProjectTaskStatus.Complete);
    }
  }
);

export const selectTasksOwner = createSelector(
  selectTasks,
  tasks => {
    if (tasks) {
      return tasks.filter(task => task.status === ProjectTaskStatus.New);
    }
  }
);

export const selectTasksBacklog = createSelector(
  selectTasks,
  tasks => {
    if (tasks) {
      return tasks.filter(task => task.status === ProjectTaskStatus.InActive);
    }
  }
);

export const selectTasksLoading = createSelector(
  selectProjectTasksFeature,
  (state: ProjectTasksState) => state.loading
);

export const selectTasksLoaded = createSelector(
  selectProjectTasksFeature,
  (state: ProjectTasksState) => state.loaded
);
