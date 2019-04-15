import { createSelector, createFeatureSelector } from '@ngrx/store';
import { UsersState, selectAllUsers } from './users.reducer';
import { AppState } from '@app/core/core.state';

export const selectUsersFeature = createFeatureSelector<UsersState>('users');

export const selectUserEntities = createSelector(
  selectUsersFeature,
  (state: UsersState) => state.users
);

export const selectUsers = createSelector(
  selectUserEntities,
  selectAllUsers
);

export const selectUsersLoading = createSelector(
  selectUsersFeature,
  (state: UsersState) => state.loading
);

export const selectUsersLoaded = createSelector(
  selectUsersFeature,
  (state: UsersState) => state.loaded
);

export interface State extends AppState {
  Users: UsersState;
}
