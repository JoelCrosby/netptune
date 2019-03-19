import { Component } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { pullIn } from '@app/core/animations/animations';
import { AuthState } from '../../../../core/auth/store/auth.reducer';
import { Store } from '@ngrx/store';
import { ActionAuthTryLogin } from '../../../../core/auth/store/auth.actions';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
  animations: [pullIn],
})
export class LoginComponent {
  hidePassword = true;

  loginGroup = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
    password: new FormControl('', [Validators.required, Validators.minLength(4)]),
  });

  get f() {
    return this.loginGroup.controls;
  }

  constructor(private router: Router, private store: Store<AuthState>) {}

  login() {
    const loginFormControl = this.loginGroup.get('email');
    const passwordFormControl = this.loginGroup.get('password');

    const username = loginFormControl ? loginFormControl.value : undefined;
    const password = passwordFormControl ? passwordFormControl.value : undefined;

    if (username && password) {
      this.store.dispatch(
        new ActionAuthTryLogin({
          username,
          password,
        })
      );
    }
  }

  onCreateAccountClicked(): void {
    this.router.navigate(['/auth/register']);
  }
}
