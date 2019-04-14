import { createSelector, createFeatureSelector } from '@ngrx/store';
import { WorkspacesState } from './workspaces.reducer';
import { AppState } from '@app/core/core.state';

export const selectWorkspacesFeature = createFeatureSelector<WorkspacesState>('workspaces');

export const selectWorkspaces = createSelector(
  selectWorkspacesFeature,
  (state: WorkspacesState) => state.Workspaces
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
