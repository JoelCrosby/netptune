import { createSelector, createFeatureSelector } from '@ngrx/store';
import { WorkspacesState, selectAllWorkspaces } from './workspaces.reducer';
import { AppState } from '@core/core.state';

export const selectWorkspacesFeature = createFeatureSelector<WorkspacesState>('workspaces');

export const selectWorkspaceEntities = createSelector(
  selectWorkspacesFeature,
  (state: WorkspacesState) => state.Workspaces
);

export const selectWorkspaces = createSelector(
  selectWorkspaceEntities,
  selectAllWorkspaces
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
  Workspaces: WorkspacesState;
}
