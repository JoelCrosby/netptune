import { AppState } from '@core/core.state';
import { createSelector, createFeatureSelector } from '@ngrx/store';
import { adapter, ProjectsState } from './projects.model';

export interface State extends AppState {
  projects: ProjectsState;
}

const selectProjectsFeature = createFeatureSelector<State, ProjectsState>('projects');

const { selectEntities, selectAll } = adapter.getSelectors();

export const selectAllProjects = createSelector(
  selectProjectsFeature,
  selectAll
);

export const selectProjectsEntities = createSelector(
  selectProjectsFeature,
  selectEntities
);

export const selectProjectsLoading = createSelector(
  selectProjectsFeature,
  (state: ProjectsState) => state.loading
);

export const selectProjectsLoaded = createSelector(
  selectProjectsFeature,
  (state: ProjectsState) => state.loaded
);
