import { selectAuthFeature } from '@core/core.state';
import { createSelector } from '@ngrx/store';
import { AuthState, UserResponse, UserToken } from './auth.models';

export const selectAuthState = createSelector(
  selectAuthFeature,
  (state: AuthState) => state
);

export const selectLoginLoading = createSelector(
  selectAuthState,
  (state: AuthState) => state.loginLoading
);

export const selectCurrentUser = createSelector(
  selectAuthState,
  (state: AuthState) => state.currentUser
);

export const selectCurrentUserId = createSelector(
  selectCurrentUser,
  (state?: UserResponse) => state?.userId
);

export const selectUserToken = createSelector(
  selectAuthState,
  (state: AuthState) => state.token
);

export const selectAuthToken = createSelector(
  selectUserToken,
  (token?: UserToken) => token?.token
);

export const selectLoginError = createSelector(
  selectAuthState,
  (state: AuthState) => state.loginError
);

export const selectCurrentUserDisplayName = createSelector(
  selectCurrentUser,
  (user?: UserResponse) => user && (user.displayName || user.email)
);

export const selectIsAuthenticated = createSelector(
  selectUserToken,
  (token?: UserToken) => {
    if (!token || !token.expires) return false;

    const expires = new Date(token.expires);

    return expires.getTime() > new Date().getTime();
  }
);

export const selectIsConfirmEmailLoading = createSelector(
  selectAuthState,
  (state: AuthState) => state.confirmEmailLoading
);

export const selectRequestPasswordResetLoading = createSelector(
  selectAuthState,
  (state: AuthState) => state.requestPasswordResetLoading
);

export const selectResetPasswordLoading = createSelector(
  selectAuthState,
  (state: AuthState) => state.resetPasswordLoading
);

export const selectRegisterLoading = createSelector(
  selectAuthState,
  (state: AuthState) => state.registerLoading
);

export const selectShowLoginError = createSelector(
  selectAuthState,
  (state: AuthState) => !!state.loginError
);
