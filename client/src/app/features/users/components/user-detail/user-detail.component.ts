import { Component, computed, inject, input } from '@angular/core';
import { SessionService } from '@core/services/session.service';
import { hasPermission } from '@core/auth/has-permission';
import { PermissionListComponent } from '@app/static/components/permission-list/permission-list.component';
import { LucideShieldCheck, LucideUserRoundX } from '@lucide/angular';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import {
  BadgeColor,
  BadgeComponent,
} from '@static/components/badge/badge.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { PERMISSIONS } from '@app/core/auth/permissions';
import { WorkspaceRole, workspaceRoleLabels } from '@core/enums/workspace-role';
import { UserCommandsService } from '@core/services/user-commands.service';
import { WorkspaceAppUser } from '@core/models/appuser';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { PanelComponent } from '@static/components/panel.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import { PanelBodyComponent } from '@static/components/panel-body.component';

@Component({
  selector: 'app-user-detail',
  imports: [
    AvatarComponent,
    BadgeComponent,
    EmptyStateComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    LucideUserRoundX,
    PanelBodyComponent,
    PanelComponent,
    PanelHeaderComponent,
    PermissionListComponent,
  ],
  template: `
    @if (user(); as user) {
      <div class="flex flex-col gap-6">
        <section app-panel surface="card">
          <header
            app-panel-body
            divider="bottom"
            class="flex flex-wrap items-center justify-between gap-x-4 gap-y-3">
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

          <app-panel-body class="max-w-sm">
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
          </app-panel-body>
        </section>

        <section app-panel surface="card">
          <app-panel-header
            density="comfortable"
            [icon]="permissionsIcon"
            i18n-heading="Heading above a member's permissions"
            heading="Permissions"
            i18n-description="Explains what a member's permissions grant"
            description="What this member can do in the workspace, on top of their role." />

          <app-permission-list [user]="user" />
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

  private readonly userCommands = inject(UserCommandsService);

  readonly user = input<WorkspaceAppUser>();
  readonly workspaceRole = WorkspaceRole;
  readonly editableRoles = [
    WorkspaceRole.viewer,
    WorkspaceRole.member,
    WorkspaceRole.admin,
  ];
  readonly canUpdateRole = hasPermission(PERMISSIONS.members.updateRole);
  readonly currentUserId = inject(SessionService).currentUserId;

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

    this.userCommands.updateRole(userId, role);
  }
}
