import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { InviteDialogComponent } from '@app/entry/dialogs/invite-dialog/invite-dialog.component';
import { first } from 'rxjs/operators';
import { Store } from '@ngrx/store';
import { inviteUsersToWorkspace } from '@core/store/users/users.actions';

@Component({
  templateUrl: './users-view.component.html',
  styleUrls: ['./users-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersViewComponent implements OnInit {
  constructor(private dialog: MatDialog, private store: Store) {}

  ngOnInit(): void {}

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
