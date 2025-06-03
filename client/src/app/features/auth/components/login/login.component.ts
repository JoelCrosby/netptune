import { ChangeDetectionStrategy, Component, OnDestroy } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import * as AuthActions from '@core/auth/store/auth.actions';
import {
  selectLoginLoading,
  selectShowLoginError,
} from '@core/auth/store/auth.selectors';
import { Store } from '@ngrx/store';
import { Observable, Subject } from 'rxjs';
import { tap } from 'rxjs/operators';

@Component({
    selector: 'app-login',
    templateUrl: './login.component.html',
    styleUrls: ['./login.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false
})
export class LoginComponent implements OnDestroy {
  authLoading$: Observable<boolean>;
  showLoginError$: Observable<boolean>;
  onDestroy$ = new Subject<void>();

  hidePassword = true;

  loginGroup = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
    password: new FormControl('', [Validators.required]),
  });

  get email() {
    return this.loginGroup.controls.email;
  }

  get password() {
    return this.loginGroup.controls.password;
  }

  constructor(private store: Store) {
    this.showLoginError$ = this.store.select(selectShowLoginError);
    this.authLoading$ = this.store.select(selectLoginLoading).pipe(
      tap((loading) => {
        if (loading) return this.loginGroup.disable();
        return this.loginGroup.enable();
      })
    );
  }

  ngOnDestroy() {
    this.store.dispatch(AuthActions.clearError({ error: 'loginError' }));

    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  login() {
    if (this.email.invalid || this.password.invalid) {
      this.email.markAllAsTouched();
      return;
    }

    const email = this.email.value as string;
    const password = this.password.value as string;

    this.store.dispatch(
      AuthActions.login({
        request: {
          email,
          password,
        },
      })
    );
  }

  onGithubSignInClicked() {
    location.href = '/api/auth/github-login';
  }
}
