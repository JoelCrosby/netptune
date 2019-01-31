import { Component } from '@angular/core';
import { Validators, FormControl, FormGroup } from '@angular/forms';
import { Router } from '@angular/router';
import { pullIn } from '../../../../animations';
import { MatSnackBar } from '@angular/material';
import { AuthService } from '../../../../services/auth/auth.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
  animations: [pullIn]
})
export class LoginComponent {

  isWorking = false;
  hidePassword = true;

  public loginFromGroup = new FormGroup({
    emailFormControl: new FormControl('', [
      Validators.required,
      Validators.email,
    ]),
    passwordFormControl: new FormControl('', [
      Validators.required,
      Validators.minLength(4),
    ])
  });

  // convenience getter for easy access to form fields
  get f() { return this.loginFromGroup.controls; }

  constructor(public authServices: AuthService, private router: Router, public snackbar: MatSnackBar) { }

  async login() {
    this.isWorking = true;

    const result = await this.authServices.login(
      this.loginFromGroup.controls['emailFormControl'].value,
      this.loginFromGroup.controls['passwordFormControl'].value
    );

    this.isWorking = false;

    if (result.isSuccess) {
      this.router.navigate(['/workspaces']);
    } else {
      this.snackbar.open(result.message, undefined, { duration: 4000 });
    }
  }

  onCreateAccountClicked(): void {
    this.router.navigate(['/auth/register']);
  }
}
