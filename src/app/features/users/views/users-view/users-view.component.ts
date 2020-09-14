import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { InviteDialogComponent } from '@entry/dialogs/invite-dialog/invite-dialog.component';
import { inviteUsersToWorkspace } from '@core/store/users/users.actions';
import { Store } from '@ngrx/store';
import { first } from 'rxjs/operators';

@Component({
  templateUrl: './users-view.component.html',
  styleUrls: ['./users-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersViewComponent {
  constructor(private dialog: MatDialog, private store: Store) {}

  onInviteUsers() {
    const dialogRef = this.dialog.open(InviteDialogComponent, {
      width: '800px',
    });

    dialogRef
      .afterClosed()
      .pipe(first())
      .subscribe({
        next: (result) => {
          if (!result?.length) return;

          this.store.dispatch(
            inviteUsersToWorkspace({ emailAddresses: result })
          );
        },
      });
  }
}
