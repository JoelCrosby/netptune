import { Component, OnInit } from '@angular/core';
import { dropIn } from '@app/core/animations/animations';
import { AppUser } from '@app/models/appuser';
import { UserService } from '@app/services/user/user.service';
import { MatDialog, MatSnackBar } from '@angular/material';
import { WorkspaceService } from '@app/services/workspace/workspace.service';

@Component({
  selector: 'app-users',
  templateUrl: './users.index.component.html',
  styleUrls: ['./users.index.component.scss'],
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
