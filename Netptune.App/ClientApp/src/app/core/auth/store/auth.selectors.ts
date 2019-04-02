import { createFeatureSelector, createSelector } from '@ngrx/store';

import { AuthState } from './auth.reducer';

export const selectAuthState = createFeatureSelector<AuthState>('auth');

export const selectIsAuthenticated = createSelector(
  selectAuthState,
  (state: AuthState) => state.isAuthenticated
);

export const selectCurrentUser = createSelector(
  selectAuthState,
  (state: AuthState) => state.currentUser
);

export const selectAuthToken = createSelector(
  selectAuthState,
  selectCurrentUser,
  (state: AuthState) => (state.currentUser ? state.currentUser.token : undefined)
);

export const selectCurrentUserDisplayName = createSelector(
  selectAuthState,
  selectCurrentUser,
  (state: AuthState) => {
    if (state.currentUser) {
      return state.currentUser.displayName || state.currentUser.email;
    }
  }
);
