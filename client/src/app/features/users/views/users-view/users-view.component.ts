import { Component, inject } from '@angular/core';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { DialogService } from '@core/services/dialog.service';
import {
  inviteUsersToWorkspace,
  loadUsers,
} from '@core/store/users/users.actions';
import { selectUsersLoading } from '@core/store/users/users.selectors';
import { dispatchForWorkspace } from '@core/util/dispatch-for-workspace';
import { InviteDialogComponent } from '@entry/dialogs/invite-dialog/invite-dialog.component';
import { Store } from '@ngrx/store';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { UserListComponent } from '@users/components/user-list/user-list.component';
import { first } from 'rxjs/operators';

@Component({
  templateUrl: './users-view.component.html',
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    SpinnerComponent,
    UserListComponent,
  ],
})
export class UsersViewComponent {
  private dialog = inject(DialogService);
  private store = inject(Store);

  loading = this.store.selectSignal(selectUsersLoading);

  constructor() {
    dispatchForWorkspace(() => loadUsers());
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
