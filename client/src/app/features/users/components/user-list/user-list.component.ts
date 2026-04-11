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
import { UserListItemComponent } from '../user-list-item/user-list-item.component';

@Component({
  selector: 'app-user-list',
  templateUrl: './user-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
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
