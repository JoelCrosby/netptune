import { createSelector, createFeatureSelector } from '@ngrx/store';
import { AppState } from '@core/core.state';
import { adapter, UsersState } from './users.model';

export const selectUsersFeature = createFeatureSelector< UsersState>(
  'users'
);

const { selectAll } = adapter.getSelectors();

export const selectUsers = createSelector(
  selectUsersFeature,
  (state: UsersState) => state
);

export const selectAllUsers = createSelector(selectUsers, selectAll);

export const selectUsersLoading = createSelector(
  selectUsersFeature,
  (state: UsersState) => state.loading && !state.loaded
);

export const selectUsersLoaded = createSelector(
  selectUsersFeature,
  (state: UsersState) => state.loaded
);

export interface State extends AppState {
  users: UsersState;
}
