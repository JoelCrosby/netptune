import { createFeatureSelector, createSelector } from '@ngrx/store';
import { AuthState, User } from './auth.models';

export const selectAuthState = createFeatureSelector<AuthState>('auth');

export const selectIsAuthenticated = createSelector(
  selectAuthState,
  (state: AuthState) => state.isAuthenticated
);

export const selectAuthLoading = createSelector(
  selectAuthState,
  (state: AuthState) => state.loading
);

export const selectCurrentUser = createSelector(
  selectAuthState,
  (state: AuthState) => state.currentUser
);

export const selectAuthToken = createSelector(
  selectCurrentUser,
  (user: User) => user && user.token
);

export const selectCurrentUserDisplayName = createSelector(
  selectCurrentUser,
  (user: User) => user && (user.displayName || user.email)
);
