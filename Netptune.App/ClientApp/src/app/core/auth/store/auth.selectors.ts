import { createFeatureSelector, createSelector } from '@ngrx/store';

import { AuthState } from './auth.reducer';

export const getAuthState = createFeatureSelector<AuthState>('auth');

export const selectIsAuthenticated = createSelector(
  getAuthState,
  (state: AuthState) => state.isAuthenticated
);

export const selectCurrentUser = createSelector(
  getAuthState,
  (state: AuthState) => state.currentUser
);

export const selectAuthToken = createSelector(
  getAuthState,
  selectCurrentUser,
  (state: AuthState) => (state.currentUser ? state.currentUser.token : undefined)
);

export const selectCurrentUserDisplayName = createSelector(
  getAuthState,
  selectCurrentUser,
  (state: AuthState) => {
    if (state.currentUser) {
      return state.currentUser.displayName || state.currentUser.email;
    }
  }
);
