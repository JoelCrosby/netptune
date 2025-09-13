import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { DialogService } from '@core/services/dialog.service';
import { MatSnackBar } from '@angular/material/snack-bar';
import { WorkspaceAppUser } from '@core/models/appuser';
import { removeUsersFromWorkspace } from '@core/store/users/users.actions';
import * as UsersSelectors from '@core/store/users/users.selectors';
import { select, Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { AsyncPipe } from '@angular/common';
import { UserListItemComponent } from '../user-list-item/user-list-item.component';
import { MatIconButton } from '@angular/material/button';
import { CdkDragHandle } from '@angular/cdk/drag-drop';
import { MatTooltip } from '@angular/material/tooltip';
import { MatMenuTrigger, MatMenu, MatMenuContent, MatMenuItem } from '@angular/material/menu';
import { MatIcon } from '@angular/material/icon';

@Component({
    selector: 'app-user-list',
    templateUrl: './user-list.component.html',
    styleUrls: ['./user-list.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [UserListItemComponent, MatIconButton, CdkDragHandle, MatTooltip, MatMenuTrigger, MatIcon, MatMenu, MatMenuContent, MatMenuItem, AsyncPipe]
})
export class UserListComponent implements OnInit {
  snackBar = inject(MatSnackBar);
  dialog = inject(DialogService);
  private store = inject(Store);

  users$!: Observable<WorkspaceAppUser[]>;

  ngOnInit() {
    this.users$ = this.store.pipe(select(UsersSelectors.selectAllUsers));
  }

  trackById(_: number, user: WorkspaceAppUser) {
    return user.id;
  }

  onRemoveClicked(user: WorkspaceAppUser) {
    if (!user) return;

    const emailAddresses = [user.email];
    this.store.dispatch(removeUsersFromWorkspace({ emailAddresses }));
  }
}
