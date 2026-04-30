import { selectAuthFeature } from '@core/core.state';
import { createSelector } from '@ngrx/store';
import { AuthState, UserResponse } from './auth.models';
import { Permission } from '../../auth/permissions';
import { WorkspaceRole } from '@app/core/enums/workspace-role';

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

export const selectLoginError = createSelector(
  selectAuthFeature,
  (state: AuthState) => state.loginError
);

export const selectCurrentUserDisplayName = createSelector(
  selectCurrentUser,
  (user?: UserResponse) => user && (user.displayName || user.email)
);

export const selectIsAuthenticated = createSelector(
  selectAuthFeature,
  (state: AuthState) => {
    if (!state.isAuthenticated || !state.tokenExpires) return false;

    return new Date(state.tokenExpires).getTime() > Date.now();
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

export const selectCurrentUserPermissions = createSelector(
  selectCurrentUser,
  (user?: UserResponse) => user?.userPermissions
);

export const selectHasPermission = (permission: Permission) =>
  createSelector(selectCurrentUserPermissions, (userPermissions) => {
    const role = userPermissions?.role;

    if (role === WorkspaceRole.owner || role === WorkspaceRole.admin) {
      return true;
    }

    return userPermissions?.permissions.includes(permission) ?? false;
  });

export const selectPermissions = createSelector(
  selectCurrentUserPermissions,
  (userPermissions) => ({
    ...userPermissions?.permissions,
    has: (permission: Permission) => {
      return userPermissions?.permissions.includes(permission);
    },
  })
);
