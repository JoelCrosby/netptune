import { Component, inject } from '@angular/core';
import { PermissionListComponent } from '@app/static/components/permission-list/permission-list.component';
import { selectUserDetail } from '@core/store/users/users.selectors';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { netptunePermissions } from '@app/core/auth/permissions';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { WorkspaceRole, workspaceRoleLabels } from '@core/enums/workspace-role';
import { updateWorkspaceRole } from '@core/store/users/users.actions';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';

@Component({
  selector: 'app-user-detail',
  imports: [
    AvatarComponent,
    PermissionListComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
  ],
  template: ` @if (user(); as user) {
      <div class="flex flex-col items-baseline gap-8 pb-96">
        <app-avatar
          [name]="user.displayName"
          [imageUrl]="user.pictureUrl"
          [isServiceAccount]="user.isServiceAccount ?? false"
          size="xl" />
        <p
          class="text-foreground/70 bg-secondary-background rounded-sm px-4 py-1 text-lg font-medium">
          {{ user.email }}
        </p>

        <div class="w-full max-w-sm">
          <app-form-select
            label="Workspace role"
            name="workspaceRole"
            [value]="user.role"
            [disabled]="!canUpdateRole() || user.role === workspaceRole.owner"
            (changed)="onRoleChanged($event)">
            @for (role of editableRoles; track role) {
              <app-form-select-option [value]="role">
                {{ roleLabel(role) }}
              </app-form-select-option>
            }
            @if (user.role === workspaceRole.owner) {
              <app-form-select-option [value]="workspaceRole.owner">
                Owner
              </app-form-select-option>
            }
          </app-form-select>
        </div>

        <h2 class="text-foreground pl-1 text-2xl">Permissions</h2>

        <div class="w-full">
          <app-permission-list />
        </div>
      </div>
    } @else {
      <p>User not found</p>
    }`,
})
export class UserDetailComponent {
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

  roleLabel(role: WorkspaceRole) {
    return workspaceRoleLabels[role];
  }

  onRoleChanged(role: WorkspaceRole) {
    const userId = this.user()?.id;
    if (!userId) return;

    this.store.dispatch(updateWorkspaceRole.init({ userId, role }));
  }
}
