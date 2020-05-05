import { AppState, selectWorkspacesFeature } from '@core/core.state';
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
  (state: WorkspacesState) => state.loading
);

export const selectWorkspacesLoaded = createSelector(
  selectWorkspacesFeature,
  (state: WorkspacesState) => state.loaded
);

export const SelectCurrentWorkspace = createSelector(
  selectWorkspacesFeature,
  (state: WorkspacesState) => state.currentWorkspace
);

export interface State extends AppState {
  workspaces: WorkspacesState;
}
