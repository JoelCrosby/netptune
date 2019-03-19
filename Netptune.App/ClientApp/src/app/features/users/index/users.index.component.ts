import { Component } from '@angular/core';
import { MatDialog, MatSnackBar } from '@angular/material';
import { dropIn } from '@app/core/animations/animations';
import { AppUser } from '@app/core/models/appuser';

@Component({
  selector: 'app-users',
  templateUrl: './users.index.component.html',
  styleUrls: ['./users.index.component.scss'],
  animations: [dropIn],
})
export class UsersComponent {
  constructor(public snackbar: MatSnackBar, public dialog: MatDialog) {}

  trackById(index: number, user: AppUser) {
    return user.id;
  }
}
