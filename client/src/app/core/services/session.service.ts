import { computed, inject, Service, signal } from '@angular/core';
import { Permission } from '@core/auth/permissions';
import { WorkspaceRole } from '@core/enums/workspace-role';
import { LoginResponse, UserResponse } from '@core/models/session';
import { UserPermissions } from '@core/models/user-permissions';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';

@Service()
export class SessionService {
  private readonly currentWorkspace = inject(CurrentWorkspaceService);

  private readonly user = signal<UserResponse | undefined>(undefined);
  private readonly signedIn = signal(false);
  private readonly expires = signal<string | undefined>(undefined);

  readonly currentUser = this.user.asReadonly();
  readonly currentUserId = computed(() => this.user()?.userId);

  readonly displayName = computed(() => {
    const user = this.user();

    return user && (user.displayName || user.email);
  });

  /** Signed in at some point, whether or not the token has since expired. */
  readonly hasAuthSession = this.signedIn.asReadonly();

  readonly isAuthenticated = computed(() => {
    const expires = this.expires();

    if (!this.signedIn() || !expires) return false;

    return new Date(expires).getTime() > Date.now();
  });

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

  /** Everything that authenticates ends here: login, register, confirm, reset, refresh. */
  establish(user: LoginResponse) {
    this.user.set(user);
    this.signedIn.set(true);
    this.expires.set(user.expires);
  }

  /** A re-read of the signed-in user, which carries their permissions but no new token. */
  setUser(user: UserResponse) {
    this.user.set(user);
    this.signedIn.set(true);
  }

  clear() {
    this.user.set(undefined);
    this.signedIn.set(false);
    this.expires.set(undefined);
  }

  can(permission: Permission): boolean {
    const userPermissions = this.userPermissions();
    const role = userPermissions?.role;

    if (role === WorkspaceRole.owner || role === WorkspaceRole.admin) {
      return true;
    }

    return userPermissions?.permissions.includes(permission) ?? false;
  }
}
