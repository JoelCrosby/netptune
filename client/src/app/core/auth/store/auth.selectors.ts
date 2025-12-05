import { selectAuthFeature } from '@core/core.state';
import { createSelector } from '@ngrx/store';
import { AuthState, UserResponse, UserToken } from './auth.models';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';

export const selectLoginLoading = createSelector(
  selectAuthFeature,
  (state: AuthState) => state.loginLoading
);

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

export const selectUserToken = createSelector(
  selectAuthFeature,
  (state: AuthState) => state.token
);

export const selectAuthToken = createSelector(
  selectUserToken,
  (token?: UserToken) => token?.token
);

export const selectAuthTokenWithWorkspaceId = createSelector(
  selectUserToken,
  selectCurrentWorkspaceIdentifier,
  (token: UserToken | undefined, workspaceId: string | undefined) => ({
    token: token?.token,
    workspaceId,
  })
);

export const selectLoginError = createSelector(
  selectAuthFeature,
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
  selectAuthFeature,
  (state: AuthState) => state.confirmEmailLoading
);

export const selectRequestPasswordResetLoading = createSelector(
  selectAuthFeature,
  (state: AuthState) => state.requestPasswordResetLoading
);

export const selectResetPasswordLoading = createSelector(
  selectAuthFeature,
  (state: AuthState) => state.resetPasswordLoading
);

export const selectRegisterLoading = createSelector(
  selectAuthFeature,
  (state: AuthState) => state.registerLoading
);

export const selectShowLoginError = createSelector(
  selectAuthFeature,
  (state: AuthState) => !!state.loginError
);
