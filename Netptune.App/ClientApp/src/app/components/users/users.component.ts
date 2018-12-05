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
    await this.userService.showInviteUserDialog();
  }

}
