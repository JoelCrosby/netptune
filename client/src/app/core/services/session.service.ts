import { computed, inject, Injectable } from '@angular/core';
import { Permission } from '@core/auth/permissions';
import { WorkspaceRole } from '@core/enums/workspace-role';
import { UserPermissions } from '@core/models/user-permissions';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import {
  selectCurrentUser,
  selectIsAuthenticated,
} from '@core/store/auth/auth.selectors';
import { Store } from '@ngrx/store';

@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly store = inject(Store);
  private readonly currentWorkspace = inject(CurrentWorkspaceService);

  readonly currentUser = this.store.selectSignal(selectCurrentUser);
  readonly isAuthenticated = this.store.selectSignal(selectIsAuthenticated);

  /** Someone reading a public workspace without signing in. */
  readonly isPublicViewer = computed(() => {
    return (
      !this.isAuthenticated() &&
      this.currentWorkspace.workspace()?.isPublic === true
    );
  });

  readonly isAssistantAvailable = computed(() => {
    if (!this.isAuthenticated() || this.isPublicViewer()) return false;

    return this.currentWorkspace.workspace()?.assistantEnabled !== false;
  });

  private readonly userPermissions = computed<UserPermissions | undefined>(
    () => {
      if (!this.isPublicViewer()) {
        return this.currentUser()?.userPermissions;
      }

      const workspace = this.currentWorkspace.workspace();

      if (!workspace) return undefined;

      return {
        userId: '',
        workspaceKey: workspace.slug,
        role: WorkspaceRole.viewer,
        permissions: workspace.publicPermissions ?? [],
      };
    }
  );

  readonly permissions = computed(() => ({
    ...this.userPermissions()?.permissions,
    has: (permission: Permission) => {
      return this.userPermissions()?.permissions.includes(permission);
    },
  }));

  can(permission: Permission): boolean {
    const userPermissions = this.userPermissions();
    const role = userPermissions?.role;

    if (role === WorkspaceRole.owner || role === WorkspaceRole.admin) {
      return true;
    }

    return userPermissions?.permissions.includes(permission) ?? false;
  }
}
