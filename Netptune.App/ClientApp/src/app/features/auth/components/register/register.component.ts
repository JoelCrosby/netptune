import { Component } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { pullIn } from '@app/core/animations/animations';
import { AppState } from '@app/core/core.state';
import { Store } from '@ngrx/store';
import { ActionAuthRegister } from '@app/core/auth/store/auth.actions';
import { selectAuthLoading } from '@app/core/auth/store/auth.selectors';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'],
  animations: [pullIn],
})
export class RegisterComponent {
  $authLoading = this.store.select(selectAuthLoading);

  constructor(private router: Router, private store: Store<AppState>) {}

  registerGroup = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
    password0: new FormControl('', [Validators.required, Validators.minLength(4)]),
    password1: new FormControl('', [Validators.required, Validators.minLength(4)]),
  });

  get f() {
    return this.registerGroup.controls;
  }

  register() {
    const loginFormControl = this.registerGroup.get('email');
    const password0FormControl = this.registerGroup.get('password0');
    const password1FormControl = this.registerGroup.get('password1');

    const username = loginFormControl ? (loginFormControl.value as string) : undefined;
    const password = password0FormControl ? (password0FormControl.value as string) : undefined;

    const passwordConfirm = password1FormControl
      ? (password1FormControl.value as string)
      : undefined;

    if (password != passwordConfirm) {
      return;
    }

    if (!username || !password) {
      return;
    }

    this.store.dispatch(
      new ActionAuthRegister({
        username,
        password,
      })
    );
  }

  backToLoginClicked(): void {
    this.router.navigate(['/auth/login']);
  }
}
