import { selectAuthFeature } from '@core/core.state';
import { createSelector } from '@ngrx/store';
import { AuthState, UserResponse } from './auth.models';

const sessionRefreshBufferMs = 60_000;

export const selectCurrentUser = createSelector(
  selectAuthFeature,
  (state: AuthState) => state.currentUser
);

export const selectRequiredCurrentUser = createSelector(
  selectAuthFeature,
  (state: AuthState) => {
    if (!state.currentUser) {
      throw new Error('current user is not defined');
    }

    return state.currentUser;
  }
);

export const selectCurrentUserId = createSelector(
  selectCurrentUser,
  (state?: UserResponse) => state?.userId
);

export const selectCurrentUserDisplayName = createSelector(
  selectCurrentUser,
  (user?: UserResponse) => user && (user.displayName || user.email)
);

export const selectIsAuthenticated = createSelector(
  selectAuthFeature,
  (state: AuthState) => {
    if (!state.isAuthenticated || !state.tokenExpires) {
      return false;
    }

    return new Date(state.tokenExpires).getTime() > Date.now();
  }
);

export const selectHasAuthSession = createSelector(
  selectAuthFeature,
  (state: AuthState) => state.isAuthenticated
);

export const selectShouldRefreshSession = createSelector(
  selectAuthFeature,
  (state: AuthState) => {
    if (!state.isAuthenticated) return false;
    if (!state.tokenExpires) return true;

    const expiresAt = new Date(state.tokenExpires).getTime();

    if (Number.isNaN(expiresAt)) return true;

    return expiresAt - sessionRefreshBufferMs <= Date.now();
  }
);
