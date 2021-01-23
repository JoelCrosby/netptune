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

export const selectProjectDetailLoading = createSelector(
  selectProjectsFeature,
  (state: ProjectsState) => state.projectDetailLoading
);

export const selectProjectDetail = createSelector(
  selectProjectsFeature,
  (state: ProjectsState) => state.projectDetail
);

export const selectProjectsLoaded = createSelector(
  selectProjectsFeature,
  (state: ProjectsState) => state.loaded
);

export const selectCurrentProject = createSelector(
  selectProjectsFeature,
  (state: ProjectsState) => state.currentProject
);

export const selectUpdateProjectLoading = createSelector(
  selectProjectsFeature,
  (state: ProjectsState) => state.projectUpdateLoading
);

export const selectProjectBoards = createSelector(
  selectProjectsFeature,
  (state: ProjectsState) => state.projectBoards
);

export const selectProjectBoardsLoading = createSelector(
  selectProjectsFeature,
  (state: ProjectsState) => state.projectBoardsLoading
);
