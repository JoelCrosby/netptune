import { createSelector, createFeatureSelector } from '@ngrx/store';
import { AppState } from '@core/core.state';
import { adapter, UsersState } from './users.model';

export const selectUsersFeature = createFeatureSelector<UsersState>('users');

const { selectAll } = adapter.getSelectors();

export const selectAllUsers = createSelector(selectUsersFeature, selectAll);

export const selectUserDetail = createSelector(
  selectUsersFeature,
  (state: UsersState) => state.userDetail
);

export const selectUserDetailLoading = createSelector(
  selectUsersFeature,
  (state: UsersState) => state.userDetailLoading
);

export const selectUserDetailLoadingError = createSelector(
  selectUsersFeature,
  (state: UsersState) => state.userDetailLoadingError
);

export interface State extends AppState {
  users: UsersState;
}
