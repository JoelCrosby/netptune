import { Component, OnInit } from '@angular/core';
import { dropIn } from '../../animations';
import { AppUser } from '../../models/appuser';
import { UserService } from '../../services/user/user.service';
import { InviteDialogComponent } from '../dialogs/invite-dialog/invite-dialog.component';
import { MatDialog, MatSnackBar } from '@angular/material';
import { WorkspaceService } from '../../services/workspace/workspace.service';

@Component({
  selector: 'app-users',
  templateUrl: './users.component.html',
  styleUrls: ['./users.component.scss'],
  animations: [dropIn]
})
export class UsersComponent implements OnInit {

  constructor(
    public userService: UserService,
    public workspaceService: WorkspaceService,
    public snackbar: MatSnackBar,
    public dialog: MatDialog) { }

  ngOnInit() {
    this.userService.refreshUsers();
  }

  trackById(index: number, user: AppUser) {
    return user.id;
  }

  async showInviteModal(): Promise<void> {
    const dialogRef = this.dialog.open(InviteDialogComponent, {
      width: '600px'
    });

    const email: string = await dialogRef.afterClosed().toPromise();

    if (!email) {
      return;
    }

    let user: AppUser = null;

    try {
      user = await this.userService.getUserByEmail(email).toPromise();
    } catch (error) {
      this.snackbar.open(`User with specified email address does not exist.`,
        null,
        { duration: 2000 });
      return null;
    }

    try {
      const userResult = await this.userService.inviteUser(user, this.workspaceService.currentWorkspace).toPromise();
      if (userResult) {
        this.userService.refreshUsers();
        this.snackbar.open(`User ${userResult.email} has been invited to this workspace.`,
          null,
          { duration: 2000 });
      }
    } catch (error) {
      this.snackbar.open(`An error has occured while trying to invite the user to this workspace.`,
        null,
        { duration: 2000 });
    }
  }

}
