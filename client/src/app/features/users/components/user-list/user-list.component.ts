import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { WorkspaceAppUser } from '@core/models/appuser';
import { DialogService } from '@core/services/dialog.service';
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
import { ListComponent } from '@static/components/list/list.component';
import { UserListItemComponent } from '../user-list-item/user-list-item.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';

@Component({
  selector: 'app-user-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ListComponent,
    UserListItemComponent,
    IconButtonComponent,
    TooltipDirective,
    LucideEllipsisVertical,
    LucideTrash2,
    LucideSend,
    DropdownMenuComponent,
    MenuItemComponent,
  ],
  template: `
    <app-list>
      @for (user of users(); track user.email) {
        <app-user-list-item
          [user]="user"
          class="mb-0.75 block overflow-hidden rounded-sm">
          <button
            class="w-10 flex-none"
            app-icon-button
            aria-label="more"
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
        </app-user-list-item>
      }
    </app-list>
  `,
})
export class UserListComponent {
  private store = inject(Store);

  dialog = inject(DialogService);
  users = this.store.selectSignal(selectAllUsers);

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
