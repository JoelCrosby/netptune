import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { netptunePermissions } from '@app/core/auth/permissions';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { WorkspaceAppUser } from '@core/models/appuser';
import {
  removeUsersFromWorkspace,
  resendInvite,
} from '@core/store/users/users.actions';
import { selectAllUsers } from '@core/store/users/users.selectors';
import {
  LucideEllipsisVertical,
  LucideTrash2,
  LucideSend,
} from '@lucide/angular';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import {
  TableComponent,
  TableEmptyCellDirective,
  TableHeaderRowDirective,
  TableHeadDirective,
  TableRowDirective,
} from '@static/components/table/table.component';

@Component({
  selector: 'app-user-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    AvatarComponent,
    CheckboxComponent,
    IconButtonComponent,
    TooltipDirective,
    LucideEllipsisVertical,
    LucideTrash2,
    LucideSend,
    DropdownMenuComponent,
    MenuItemComponent,
    TableComponent,
    TableEmptyCellDirective,
    TableHeaderRowDirective,
    TableHeadDirective,
    TableRowDirective,
  ],
  template: `
    <app-table
      containerClass="h-[calc(100vh-200px)] min-h-16 overflow-auto"
      tableClass="min-w-[720px] table-fixed">
      <thead appTableHead [sticky]="true">
        <tr appTableHeaderRow>
          <th class="w-12 px-4 py-3"></th>
          <th class="w-64 px-4 py-3">User</th>
          <th class="px-4 py-3">Email</th>
          <th class="w-32 px-4 py-3">Status</th>
          <th class="w-14 px-2 py-3"></th>
        </tr>
      </thead>
      <tbody>
        @for (user of users(); track user.email) {
          <tr appTableRow class="bg-card">
            <td class="px-4 py-2 align-middle">
              <app-checkbox />
            </td>
            <td class="px-4 py-2 align-middle">
              <div class="flex min-w-0 items-center gap-3">
                <app-avatar
                  class="flex-none"
                  [imageUrl]="user.pictureUrl"
                  [name]="user.displayName"
                  size="sm" />
                <a
                  [routerLink]="routerLink(user)"
                  class="min-w-0 truncate text-sm font-medium"
                  [class.pointer-events-none]="!routerLink(user)">
                  {{ user.displayName }}
                </a>
              </div>
            </td>
            <td class="px-4 py-2 align-middle">
              <a
                [routerLink]="routerLink(user)"
                class="text-foreground/60 block truncate text-sm"
                [class.pointer-events-none]="!routerLink(user)">
                {{ user.email }}
              </a>
            </td>
            <td class="px-4 py-2 align-middle">
              @if (user.isPending) {
                <span
                  class="bg-muted text-muted-foreground rounded-full px-1.5 py-0.5 text-sm">
                  Pending
                </span>
              } @else if (user.isWorkspaceOwner) {
                <span
                  class="bg-primary/10 text-primary rounded-full px-1.5 py-0.5 text-sm">
                  Owner
                </span>
              } @else {
                <span class="text-muted text-sm">Member</span>
              }
            </td>
            <td class="px-2 align-middle">
              <button
                class="w-10 flex-none"
                app-icon-button
                type="button"
                aria-label="User actions"
                appTooltip="click for more options"
                (click)="menu.toggle($any($event.currentTarget))">
                <svg
                  lucideEllipsisVertical
                  class="text-foreground/30 h-4 w-4"></svg>
              </button>

              <app-dropdown-menu #menu xPosition="before">
                @if (user.isPending) {
                  <button
                    app-menu-item
                    (click)="onResendClicked(user); menu.close()">
                    <svg lucideSend class="h-4 w-4"></svg>
                    <span>Resend invite</span>
                  </button>
                }
                @if (!user.isWorkspaceOwner) {
                  <button
                    app-menu-item
                    (click)="onRemoveClicked(user); menu.close()">
                    <svg lucideTrash2 class="h-4 w-4"></svg>
                    <span>Remove user from workspace</span>
                  </button>
                }
              </app-dropdown-menu>
            </td>
          </tr>
        } @empty {
          <tr>
            <td appTableEmptyCell colspan="5">No users to display.</td>
          </tr>
        }
      </tbody>
    </app-table>
  `,
})
export class UserListComponent {
  private store = inject(Store);

  users = this.store.selectSignal(selectAllUsers);
  canReadUsers = this.store.selectSignal(
    selectHasPermission(netptunePermissions.members.read)
  );

  routerLink(user: WorkspaceAppUser) {
    if (user.isPending) {
      return null;
    }

    return this.canReadUsers() ? ['.', user.id] : null;
  }

  onRemoveClicked(user: WorkspaceAppUser) {
    if (!user) return;

    const emailAddresses = [user.email];
    this.store.dispatch(removeUsersFromWorkspace({ emailAddresses }));
  }

  onResendClicked(user: WorkspaceAppUser) {
    if (!user?.email) return;

    this.store.dispatch(resendInvite({ email: user.email }));
  }
}
