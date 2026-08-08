import { Component, inject, signal } from '@angular/core';
import { DialogService } from '@core/services/dialog.service';
import { UserCommandsService } from '@core/services/user-commands.service';
import { InviteDialogComponent } from '@entry/dialogs/invite-dialog/invite-dialog.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { UserListComponent } from '@users/components/user-list/user-list.component';
import { first } from 'rxjs/operators';

@Component({
  selector: 'app-users-view',
  templateUrl: './users-view.component.html',
  imports: [PageContainerComponent, PageHeaderComponent, UserListComponent],
})
export class UsersViewComponent {
  private dialog = inject(DialogService);
  private userCommands = inject(UserCommandsService);

  readonly count = signal<number | null>(null);

  onInviteUsers() {
    const dialogRef = this.dialog.open<string[]>(InviteDialogComponent, {
      width: '800px',
    });

    dialogRef.closed.pipe(first()).subscribe({
      next: (result) => {
        if (!result?.length) return;

        this.userCommands.invite(result);
      },
    });
  }
}
