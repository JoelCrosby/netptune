import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth/auth.service';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { MatDialog } from '@angular/material';
import { ConfirmDialogComponent } from '../dialogs/confirm-dialog/confirm-dialog.component';
import { UserService } from '../../services/user/user.service';
import { AppUser } from '../../models/appuser';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.scss']
})
export class ProfileComponent implements OnInit {

  constructor(
    public authService: AuthService,
    public userService: UserService,
    public dialog: MatDialog) { }

  profileFromGroup = new FormGroup({

    firstNameFormControl: new FormControl('', [
    ]),

    lastNameFormControl: new FormControl('', [
    ]),

    userNameFormControl: new FormControl('', [
      Validators.required,
    ]),

    emailFormControl: new FormControl('', [
      Validators.required,
      Validators.email
    ]),


  });

  ngOnInit() {
    this.userService.getUser().subscribe((user: AppUser) => {

      this.profileFromGroup.controls['firstNameFormControl'].setValue(user.firstName);
      this.profileFromGroup.controls['lastNameFormControl'].setValue(user.lasName);
      this.profileFromGroup.controls['userNameFormControl'].setValue(user.userName);
      this.profileFromGroup.controls['emailFormControl'].setValue(user.email);

    });
  }

  saveChangesClicked(): void {

  }

  showLogOutModal(): void {

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '600px',
      data: {
        title: 'Log out',
        content: 'Are you sure you wish to log out?',
        confirm: 'Log Out'
      }
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result) {
        this.authService.logout();
      }
    });
  }

}
