import { Component, inject } from '@angular/core';
import { DialogService } from '@core/services/dialog.service';
import { UserCommandsService } from '@core/services/user-commands.service';
import { InviteDialogComponent } from '@entry/dialogs/invite-dialog/invite-dialog.component';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { UserListComponent } from '@users/components/user-list/user-list.component';

@Component({
  selector: 'app-users-view',
  templateUrl: './users-view.component.html',
  imports: [
    PageBodyComponent,
    PageContainerComponent,
    PageHeaderComponent,
    UserListComponent,
  ],
})
export class UsersViewComponent {
  private dialog = inject(DialogService);
  private userCommands = inject(UserCommandsService);

  async onInviteUsers() {
    const result = await this.dialog.openForResult<string[]>(
      InviteDialogComponent,
      { width: '800px' }
    );

    if (!result?.length) return;

    this.userCommands.invite(result);
  }
}
