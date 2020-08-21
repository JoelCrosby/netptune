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
    email: new FormControl('', [Validators.required, Validators.email]),
    password: new FormControl('', [Validators.required]),
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
    if (this.email.invalid || this.password.invalid) {
      this.email.markAsDirty();
      this.password.markAsDirty();

      return;
    }

    const email = this.email.value as string;
    const password = this.password.value as string;

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
