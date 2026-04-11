import { CdkDragHandle } from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatIconButton } from '@angular/material/button';
import {
  MatMenu,
  MatMenuContent,
  MatMenuItem,
  MatMenuTrigger,
} from '@angular/material/menu';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { WorkspaceAppUser } from '@core/models/appuser';
import { DialogService } from '@core/services/dialog.service';
import { removeUsersFromWorkspace } from '@core/store/users/users.actions';
import { selectAllUsers } from '@core/store/users/users.selectors';
import { LucideGripVertical, LucideTrash2 } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { ListComponent } from '@static/components/list/list.component';
import { UserListItemComponent } from '../user-list-item/user-list-item.component';

@Component({
  selector: 'app-user-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ListComponent,
    UserListItemComponent,
    MatIconButton,
    CdkDragHandle,
    TooltipDirective,
    MatMenuTrigger,
    LucideGripVertical,
    LucideTrash2,
    MatMenu,
    MatMenuContent,
    MatMenuItem,
  ],
  template: `
    <app-list>
      @for (user of users(); track user.id) {
        <app-user-list-item [user]="user">
          <button
            class="flex-none w-10"
            cdkDragHandle
            mat-icon-button
            aria-label="more"
            appTooltip="click for more options. click and hold to drag task"
            [matMenuTriggerData]="{ user: user }"
            [matMenuTriggerFor]="menu"
          >
            <svg lucideGripVertical class="h-4 w-4 text-foreground/30"></svg>
          </button>

          <mat-menu #menu="matMenu">
            <ng-template matMenuContent>
              @if (!user.isWorkspaceOwner) {
                <button mat-menu-item (click)="onRemoveClicked(user)">
                  <svg lucideTrash2 class="h-4 w-4"></svg>
                  <span>Remove user from workspace</span>
                </button>
              }
            </ng-template>
          </mat-menu>
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
