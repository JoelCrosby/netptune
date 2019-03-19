import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialog, MatSnackBar } from '@angular/material';
import { ConfirmDialogComponent } from '@app/dialogs/confirm-dialog/confirm-dialog.component';
import { AppUser } from '@app/core/models/appuser';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.index.component.html',
  styleUrls: ['./profile.index.component.scss'],
})
export class ProfileComponent implements OnInit {
  constructor(public snackbar: MatSnackBar, public dialog: MatDialog) {}

  profileFromGroup = new FormGroup({
    firstNameFormControl: new FormControl('', []),
    lastNameFormControl: new FormControl('', []),
    emailFormControl: new FormControl('', [Validators.required, Validators.email]),
  });

  async ngOnInit(): Promise<void> {}

  async saveChangesClicked(): Promise<void> {}

  async showLogOutModal(): Promise<void> {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '600px',
      data: {
        title: 'Log out',
        content: 'Are you sure you wish to log out?',
        confirm: 'Log Out',
      },
    });

    const result = await dialogRef.afterClosed().toPromise();
    if (result) {
    }
  }
}
