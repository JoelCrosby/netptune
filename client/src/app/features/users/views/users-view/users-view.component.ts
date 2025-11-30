import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  inject,
} from '@angular/core';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { DialogService } from '@core/services/dialog.service';
import {
  inviteUsersToWorkspace,
  loadUsers,
} from '@core/store/users/users.actions';
import { selectUsersLoading } from '@core/store/users/users.selectors';
import { InviteDialogComponent } from '@entry/dialogs/invite-dialog/invite-dialog.component';
import { Store } from '@ngrx/store';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { UserListComponent } from '@users/components/user-list/user-list.component';
import { first } from 'rxjs/operators';

@Component({
  templateUrl: './users-view.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    MatProgressSpinner,
    UserListComponent,
  ],
})
export class UsersViewComponent implements AfterViewInit {
  private dialog = inject(DialogService);
  private store = inject(Store);

  loading = this.store.selectSignal(selectUsersLoading);

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
