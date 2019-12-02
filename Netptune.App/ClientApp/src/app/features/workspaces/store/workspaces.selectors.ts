import { createSelector, createFeatureSelector } from '@ngrx/store';
import { AppState } from '@core/core.state';
import { adapter, WorkspacesState } from './workspaces.model';

export const selectWorkspacesFeature = createFeatureSelector<
  State,
  WorkspacesState
>('workspaces');

const { selectEntities, selectAll } = adapter.getSelectors();

export const selectWorkspaces = createSelector(
  selectWorkspacesFeature,
  (state: WorkspacesState) => state.Workspaces
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
