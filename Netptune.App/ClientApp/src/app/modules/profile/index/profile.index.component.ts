import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialog, MatSnackBar } from '@angular/material';
import { UserService } from '../../../services/user/user.service';
import { ConfirmDialogComponent } from '../../../dialogs/confirm-dialog/confirm-dialog.component';
import { AppUser } from '../../../models/appuser';
import { AuthService } from '../../../services/auth/auth.service';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.index.component.html',
  styleUrls: ['./profile.index.component.scss']
})
export class ProfileComponent implements OnInit {

  constructor(
    public authService: AuthService,
    public userService: UserService,
    public snackbar: MatSnackBar,
    public dialog: MatDialog) { }

  profileFromGroup = new FormGroup({
    firstNameFormControl: new FormControl('', [
    ]),
    lastNameFormControl: new FormControl('', [
    ]),
    emailFormControl: new FormControl('', [
      Validators.required,
      Validators.email
    ]),
  });

  async ngOnInit(): Promise<void> {
    const user = await this.userService.getUser().toPromise();

    this.profileFromGroup.controls['firstNameFormControl'].setValue(user.firstName);
    this.profileFromGroup.controls['lastNameFormControl'].setValue(user.lastName);
    this.profileFromGroup.controls['emailFormControl'].setValue(user.email);
  }

  async saveChangesClicked(): Promise<void> {

    const user: AppUser = JSON.parse(JSON.stringify(this.userService.currentUser));

    user.firstName = this.profileFromGroup.controls['firstNameFormControl'].value;
    user.lastName = this.profileFromGroup.controls['lastNameFormControl'].value;
    user.email = this.profileFromGroup.controls['emailFormControl'].value;

    const result = await this.userService.updateUser(user);

    if (result.isSuccess) {
      this.snackbar.open(`Changes applied.`, undefined, { duration: 2000 });
    } else {
      this.snackbar.open(result.message, undefined, { duration: 2000 });
    }
  }

  async showLogOutModal(): Promise<void> {

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '600px',
      data: {
        title: 'Log out',
        content: 'Are you sure you wish to log out?',
        confirm: 'Log Out'
      }
    });

    const result = await dialogRef.afterClosed().toPromise();
    if (result) {
      this.authService.logout();
    }
  }

}
