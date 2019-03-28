import { createSelector, createFeatureSelector } from '@ngrx/store';
import { ProjectsState } from './projects.reducer';
import { AppState } from '../../../core/core.state';

export const FEATURE_NAME = 'projects';
export const selectExports = createFeatureSelector<State, ProjectsState>(FEATURE_NAME);

export const selectProjects = createSelector(
  selectExports,
  (state: ProjectsState) => state.projects
);

export const selectProjectsLoading = createSelector(
  selectExports,
  (state: ProjectsState) => state.loading
);

export const selectProjectsLoaded = createSelector(
  selectExports,
  (state: ProjectsState) => state.loaded
);

export interface State extends AppState {
  projects: ProjectsState;
}
