import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
} from '@angular/core';
import { DialogService } from '@core/services/dialog.service';
import {
  inviteUsersToWorkspace,
  loadUsers,
} from '@core/store/users/users.actions';
import { selectUsersLoading } from '@core/store/users/users.selectors';
import { InviteDialogComponent } from '@entry/dialogs/invite-dialog/invite-dialog.component';
import { Store } from '@ngrx/store';
import { first } from 'rxjs/operators';
import { PageContainerComponent } from '../../../../static/components/page-container/page-container.component';
import { PageHeaderComponent } from '../../../../static/components/page-header/page-header.component';
import { NgIf, AsyncPipe } from '@angular/common';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { UserListComponent } from '../../components/user-list/user-list.component';

@Component({
    templateUrl: './users-view.component.html',
    styleUrls: ['./users-view.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [PageContainerComponent, PageHeaderComponent, NgIf, MatProgressSpinner, UserListComponent, AsyncPipe]
})
export class UsersViewComponent implements AfterViewInit {
  loading$ = this.store.select(selectUsersLoading);

  constructor(
    private dialog: DialogService,
    private store: Store
  ) {}

  ngAfterViewInit() {
    this.store.dispatch(loadUsers());
  }

  onInviteUsers() {
    const dialogRef = this.dialog.open<string[]>(InviteDialogComponent, {
      width: '800px',
    });

    dialogRef.closed.pipe(first()).subscribe({
      next: (result) => {
        if (!result?.length) return;

        this.store.dispatch(inviteUsersToWorkspace({ emailAddresses: result }));
      },
    });
  }
}
