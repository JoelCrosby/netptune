import { selectAuthFeature } from '@core/core.state';
import { createSelector } from '@ngrx/store';
import { AuthState, UserResponse } from './auth.models';
import { Permission } from '../../auth/permissions';
import { UserPermissions } from '@app/core/models/user-permissions';
import { WorkspaceRole } from '@app/core/enums/workspace-role';
import { selectCurrentWorkspace } from '../workspaces/workspaces.selectors';

const sessionRefreshBufferMs = 60_000;

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

export const selectIsPublicViewer = createSelector(
  selectIsAuthenticated,
  selectCurrentWorkspace,
  (isAuthenticated, workspace) =>
    !isAuthenticated && workspace?.isPublic === true
);

export const selectPublicViewerPermissions = createSelector(
  selectIsPublicViewer,
  selectCurrentWorkspace,
  (isPublicViewer, workspace): UserPermissions | undefined => {
    if (!isPublicViewer || !workspace) return undefined;

    return {
      userId: '',
      workspaceKey: workspace.slug,
      role: WorkspaceRole.viewer,
      permissions: workspace.publicPermissions ?? [],
    };
  }
);

export const selectCurrentUserPermissions = createSelector(
  selectCurrentUser,
  selectPublicViewerPermissions,
  (user: UserResponse | undefined, publicPermissions) =>
    user?.userPermissions ?? publicPermissions
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
