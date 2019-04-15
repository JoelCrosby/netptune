import { createSelector, createFeatureSelector } from '@ngrx/store';
import { ProjectsState, selectAllProjects } from './projects.reducer';
import { AppState } from '@app/core/core.state';

export const selectProjectsFeature = createFeatureSelector<ProjectsState>('projects');

export const selectProjectsEntity = createSelector(
  selectProjectsFeature,
  (state: ProjectsState) => state.projects
);

export const selectProjects = createSelector(
  selectProjectsEntity,
  selectAllProjects
);

export const selectProjectsLoading = createSelector(
  selectProjectsFeature,
  (state: ProjectsState) => state.loading
);

export const selectProjectsLoaded = createSelector(
  selectProjectsFeature,
  (state: ProjectsState) => state.loaded
);

export interface State extends AppState {
  projects: ProjectsState;
}
