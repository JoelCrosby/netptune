import { AppState, selectWorkspacesFeature } from '@core/core.state';
import { createSelector } from '@ngrx/store';
import { adapter, WorkspacesState } from './workspaces.model';

const { selectEntities, selectAll } = adapter.getSelectors();

export const selectWorkspaces = createSelector(
  selectWorkspacesFeature,
  (state: WorkspacesState) => state
);

export const selectAllWorkspaces = createSelector(selectWorkspaces, selectAll);

export const selectWorkspacesEntities = createSelector(
  selectWorkspaces,
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

export interface State extends AppState {
  workspaces: WorkspacesState;
}
