import { createFeatureSelector, createSelector } from '@ngrx/store';
import { AuthState, User } from './auth.models';

export const selectAuthState = createFeatureSelector<AuthState>('auth');

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

export const selectIsAuthenticated = createSelector(
  selectCurrentUser,
  (user: User) => {
    if (!user || !user.expires) return false;

    const expires = new Date(user.expires);

    return expires.getTime() > new Date().getTime();
  }
);
