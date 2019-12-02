import { Component, OnDestroy } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { pullIn } from '@core/animations/animations';
import { AuthState } from '@core/auth/store/auth.reducer';
import { Store } from '@ngrx/store';
import { ActionAuthTryLogin, AuthActionTypes, ActionAuthLoginFail } from '@core/auth/store/auth.actions';
import { selectAuthLoading } from '@core/auth/store/auth.selectors';
import { Actions, ofType } from '@ngrx/effects';
import { Subject } from 'rxjs';
import { takeUntil, tap } from 'rxjs/operators';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss', '../../auth.styles.scss'],
  animations: [pullIn],
})
export class LoginComponent implements OnDestroy {
  destroyed$ = new Subject<boolean>();
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

  constructor(private router: Router, private store: Store<AuthState>, updates$: Actions) {
    updates$
      .pipe(
        ofType<ActionAuthLoginFail>(AuthActionTypes.LOGIN_FAIL),
        takeUntil(this.destroyed$),
        tap(() => this.loginGroup.enable())
      )
      .subscribe();
  }

  ngOnDestroy() {
    this.destroyed$.next(true);
    this.destroyed$.complete();
  }

  login() {
    const loginFormControl = this.email;
    const passwordFormControl = this.password;

    const email = loginFormControl ? loginFormControl.value : undefined;
    const password = passwordFormControl ? passwordFormControl.value : undefined;

    this.loginGroup.disable();

    if ((!email || !password) && passwordFormControl) {
      passwordFormControl.reset();
      this.loginGroup.enable();
      return;
    }

    this.store.dispatch(
      new ActionAuthTryLogin({
        email,
        password,
      })
    );
  }

  onCreateAccountClicked(): void {
    this.router.navigate(['/auth/register']);
  }
}
