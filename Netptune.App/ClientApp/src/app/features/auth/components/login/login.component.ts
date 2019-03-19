import { Component } from '@angular/core';
import { Validators, FormControl, FormGroup } from '@angular/forms';
import { Router } from '@angular/router';
import { pullIn } from '@app/core/animations/animations';
import { MatSnackBar } from '@angular/material';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
  animations: [pullIn],
})
export class LoginComponent {
  isWorking = false;
  hidePassword = true;

  loginFromGroup = new FormGroup({
    emailFormControl: new FormControl('', [Validators.required, Validators.email]),
    passwordFormControl: new FormControl('', [Validators.required, Validators.minLength(4)]),
  });

  get f() {
    return this.loginFromGroup.controls;
  }

  constructor(private router: Router, public snackbar: MatSnackBar) {}

  async login() {}

  onCreateAccountClicked(): void {
    this.router.navigate(['/auth/register']);
  }
}
