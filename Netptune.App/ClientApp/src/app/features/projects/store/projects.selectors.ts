import { createSelector, createFeatureSelector } from '@ngrx/store';
import { ProjectsState } from './projects.reducer';
import { AppState } from '@app/core/core.state';

export const selectProjectsFeature = createFeatureSelector<ProjectsState>('projects');

export const selectProjects = createSelector(
  selectProjectsFeature,
  (state: ProjectsState) => state.projects
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
