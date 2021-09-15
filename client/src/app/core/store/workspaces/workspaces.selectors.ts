import { selectWorkspacesFeature } from '@core/core.state';
import { Workspace } from '@core/models/workspace';
import { createSelector } from '@ngrx/store';
import { adapter, WorkspacesState } from './workspaces.model';

const { selectEntities, selectAll } = adapter.getSelectors();

export const selectAllWorkspaces = createSelector(
  selectWorkspacesFeature,
  selectAll
);

export const selectWorkspacesEntities = createSelector(
  selectWorkspacesFeature,
  selectEntities
);

export const selectWorkspacesLoading = createSelector(
  selectWorkspacesFeature,
  (state: WorkspacesState) => state.loading && !state.loaded
);

export const selectWorkspacesLoaded = createSelector(
  selectWorkspacesFeature,
  (state: WorkspacesState) => state.loaded
);

export const selectCurrentWorkspace = createSelector(
  selectWorkspacesFeature,
  (state: WorkspacesState) => state.currentWorkspace
);

export const selectCurrentWorkspaceIdentifier = createSelector(
  selectCurrentWorkspace,
  (state?: Workspace) => state?.slug
);

export const selectIsSlugUnique = createSelector(
  selectWorkspacesFeature,
  (state: WorkspacesState) => state.isSlugUnique?.isUnique
);

export const selectIsSlugUniqueLoading = createSelector(
  selectWorkspacesFeature,
  (state: WorkspacesState) => state.isSlugUniqueLoading
);

export const selectIsSlugTaken = createSelector(
  selectIsSlugUnique,
  (state?: boolean) => state !== undefined && !state
);
