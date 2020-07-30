import { ChangeDetectionStrategy, Component, OnDestroy } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import * as AuthActions from '@core/auth/store/auth.actions';
import { AuthState } from '@core/auth/store/auth.models';
import { selectAuthLoading } from '@core/auth/store/auth.selectors';
import { Actions, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { Subject } from 'rxjs';
import { takeUntil, tap } from 'rxjs/operators';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent implements OnDestroy {
  onDestroy$ = new Subject();
  $authLoading = this.store.select(selectAuthLoading);
  hidePassword = true;

  loginGroup = new FormGroup({
    email: new FormControl('', [, Validators.email]),
    password: new FormControl(),
  });

  get email() {
    return this.loginGroup.get('email');
  }

  get password() {
    return this.loginGroup.get('password');
  }

  constructor(private store: Store<AuthState>, updates$: Actions) {
    updates$
      .pipe(
        ofType(AuthActions.loginFail),
        takeUntil(this.onDestroy$),
        tap(() => this.loginGroup.enable())
      )
      .subscribe();
  }

  ngOnDestroy() {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  login() {
    const loginFormControl = this.email;
    const passwordFormControl = this.password;

    const email = loginFormControl ? loginFormControl.value : undefined;
    const password = passwordFormControl
      ? passwordFormControl.value
      : undefined;

    this.loginGroup.disable();

    if ((!email || !password) && passwordFormControl) {
      passwordFormControl.reset();
      this.loginGroup.enable();
      return;
    }

    this.store.dispatch(
      AuthActions.tryLogin({
        request: {
          email,
          password,
        },
      })
    );
  }
}
