import { CdkDragHandle } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { WorkspaceAppUser } from '@core/models/appuser';
import { DialogService } from '@core/services/dialog.service';
import { removeUsersFromWorkspace } from '@core/store/users/users.actions';
import { selectAllUsers } from '@core/store/users/users.selectors';
import { LucideGripVertical, LucideTrash2 } from '@lucide/angular';
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
    CdkDragHandle,
    TooltipDirective,
    LucideGripVertical,
    LucideTrash2,
    DropdownMenuComponent,
    MenuItemComponent,
  ],
  template: `
    <app-list>
      @for (user of users(); track user.id) {
        <app-user-list-item [user]="user">
          <button
            class="flex-none w-10"
            cdkDragHandle
            app-icon-button
            aria-label="more"
            appTooltip="click for more options. click and hold to drag task"
            (click)="menu.toggle($any($event.currentTarget))"
          >
            <svg lucideGripVertical class="h-4 w-4 text-foreground/30"></svg>
          </button>

          <app-dropdown-menu #menu xPosition="before">
            @if (!user.isWorkspaceOwner) {
              <button app-menu-item (click)="onRemoveClicked(user); menu.close()">
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
}
