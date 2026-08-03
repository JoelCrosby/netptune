import { Component, computed, inject } from '@angular/core';
import { PermissionListComponent } from '@app/static/components/permission-list/permission-list.component';
import { selectUserDetail } from '@core/store/users/users.selectors';
import { Store } from '@ngrx/store';
import { LucideShieldCheck, LucideUserRoundX } from '@lucide/angular';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import {
  BadgeColor,
  BadgeComponent,
} from '@static/components/badge/badge.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
import { netptunePermissions } from '@app/core/auth/permissions';
import {
  selectCurrentUserId,
  selectHasPermission,
} from '@app/core/store/auth/auth.selectors';
import { WorkspaceRole, workspaceRoleLabels } from '@core/enums/workspace-role';
import { updateWorkspaceRole } from '@core/store/users/users.actions';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';

@Component({
  selector: 'app-user-detail',
  imports: [
    AvatarComponent,
    BadgeComponent,
    EmptyStateComponent,
    IconTileComponent,
    LucideUserRoundX,
    PermissionListComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
  ],
  template: `
    @if (user(); as user) {
      <div class="flex flex-col gap-6">
        <section
          class="border-border bg-card overflow-hidden rounded-lg border shadow-sm">
          <header
            class="border-border flex flex-wrap items-center justify-between gap-x-4 gap-y-3 border-b px-6 py-5">
            <div class="flex min-w-0 items-center gap-4">
              <app-avatar
                [name]="user.displayName"
                [imageUrl]="user.pictureUrl"
                [isServiceAccount]="user.isServiceAccount ?? false"
                size="lg" />

              <div class="min-w-0">
                <h2 class="font-overpass truncate text-base font-semibold">
                  {{ user.displayName }}
                </h2>
                <p class="text-muted truncate text-sm">{{ user.email }}</p>
              </div>
            </div>

            <app-badge [color]="roleBadgeColor()" shape="rounded">
              {{ roleLabel(user.role) }}
            </app-badge>
          </header>

          <div class="max-w-sm px-6 py-5">
            <app-form-select
              i18n-label="Label of the member role field"
              label="Workspace role"
              name="workspaceRole"
              [noMargin]="true"
              [value]="user.role"
              [disabled]="
                !canUpdateRole() ||
                isSelf() ||
                user.role === workspaceRole.owner
              "
              [hint]="roleHint()"
              (changed)="onRoleChanged($event)">
              @for (role of editableRoles; track role) {
                <app-form-select-option [value]="role">
                  {{ roleLabel(role) }}
                </app-form-select-option>
              }
              @if (user.role === workspaceRole.owner) {
                <app-form-select-option [value]="workspaceRole.owner">
                  <span i18n="Badge marking the workspace owner">Owner</span>
                </app-form-select-option>
              }
            </app-form-select>
          </div>
        </section>

        <section
          class="border-border bg-card overflow-hidden rounded-lg border shadow-sm">
          <header class="border-border border-b px-6 py-5">
            <div class="flex min-w-0 items-center gap-3">
              <app-icon-tile [icon]="permissionsIcon" />

              <div class="min-w-0">
                <h2
                  class="font-overpass text-base font-semibold"
                  i18n="Heading above a member's permissions">
                  Permissions
                </h2>
                <p
                  class="text-muted mt-1 text-sm"
                  i18n="Explains what a member's permissions grant">
                  What this member can do in the workspace, on top of their
                  role.
                </p>
              </div>
            </div>
          </header>

          <app-permission-list />
        </section>
      </div>
    } @else {
      <app-empty-state
        i18n-title="Shown when a member cannot be found"
        title="User not found"
        i18n-description="Advice shown when a member cannot be found"
        description="They may have been removed from this workspace.">
        <svg emptyStateIcon lucideUserRoundX class="h-8 w-8"></svg>
      </app-empty-state>
    }
  `,
})
export class UserDetailComponent {
  protected readonly permissionsIcon = LucideShieldCheck;

  readonly store = inject(Store);
  user = this.store.selectSignal(selectUserDetail);
  readonly workspaceRole = WorkspaceRole;
  readonly editableRoles = [
    WorkspaceRole.viewer,
    WorkspaceRole.member,
    WorkspaceRole.admin,
  ];
  readonly canUpdateRole = this.store.selectSignal(
    selectHasPermission(netptunePermissions.members.updateRole)
  );
  readonly currentUserId = this.store.selectSignal(selectCurrentUserId);

  readonly isSelf = computed(() => {
    const user = this.user();
    return !!user && user.id === this.currentUserId();
  });

  readonly roleHint = computed(() => {
    if (!this.isSelf()) return undefined;

    return $localize`:Explains why a member cannot edit their own role:You cannot change your own workspace role`;
  });

  readonly roleBadgeColor = computed<BadgeColor>(() => {
    return this.user()?.role === WorkspaceRole.owner ? 'primary' : 'neutral';
  });

  roleLabel(role: WorkspaceRole) {
    return workspaceRoleLabels[role];
  }

  onRoleChanged(role: WorkspaceRole) {
    const userId = this.user()?.id;
    if (!userId) return;

    this.store.dispatch(updateWorkspaceRole.init({ userId, role }));
  }
}
