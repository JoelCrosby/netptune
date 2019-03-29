import { createFeatureSelector, createSelector } from '@ngrx/store';
import { ProjectTasksState } from './project-tasks.reducer';
import { pipe } from 'rxjs';
import { map, filter, tap, switchMap, flatMap } from 'rxjs/operators';
import { ProjectTaskStatus } from '../../../core/enums/project-task-status';
import { ProjectTaskDto } from '../../../core/models/view-models/project-task-dto';

export const selectProjectTasksFeature = createFeatureSelector<ProjectTasksState>('project-tasks');

export const selectTasks = createSelector(
  selectProjectTasksFeature,
  (state: ProjectTasksState) => state.tasks
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
