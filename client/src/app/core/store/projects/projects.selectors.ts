import { selectProjectsFeature } from '@core/core.state';
import { createSelector } from '@ngrx/store';
import { adapter, ProjectsState } from './projects.model';

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
  (state: ProjectsState) => state.loading && !state.loaded
);

export const selectProjectsLoaded = createSelector(
  selectProjectsFeature,
  (state: ProjectsState) => state.loaded
);

export const selectCurrentProject = createSelector(
  selectProjectsFeature,
  (state: ProjectsState) => state.currentProject
);
