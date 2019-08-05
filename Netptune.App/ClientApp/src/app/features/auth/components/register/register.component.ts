import { Component, OnDestroy } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { pullIn } from '@app/core/animations/animations';
import { AppState } from '@app/core/core.state';
import { Store } from '@ngrx/store';
import {
  ActionAuthRegister,
  ActionAuthRegisterFail,
  AuthActionTypes,
} from '@app/core/auth/store/auth.actions';
import { selectAuthLoading } from '@app/core/auth/store/auth.selectors';
import { Actions, ofType } from '@ngrx/effects';
import { Subject } from 'rxjs';
import { takeUntil, tap } from 'rxjs/operators';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss', '../../auth.styles.scss'],
  animations: [pullIn],
})
export class RegisterComponent implements OnDestroy {
  destroyed$ = new Subject<boolean>();
  $authLoading = this.store.select(selectAuthLoading);

  registerGroup = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
    password0: new FormControl('', [Validators.required, Validators.minLength(4)]),
    password1: new FormControl('', [Validators.required, Validators.minLength(4)]),
  });

  get f() {
    return this.registerGroup.controls;
  }

  constructor(private router: Router, private store: Store<AppState>, updates$: Actions) {
    updates$
      .pipe(
        ofType<ActionAuthRegisterFail>(AuthActionTypes.REGISTER_FAIL),
        takeUntil(this.destroyed$),
        tap(() => this.registerGroup.enable())
      )
      .subscribe();
  }

  ngOnDestroy() {
    this.destroyed$.next(true);
    this.destroyed$.complete();
  }

  register() {
    const loginFormControl = this.registerGroup.get('email');
    const password0FormControl = this.registerGroup.get('password0');
    const password1FormControl = this.registerGroup.get('password1');

    const email = loginFormControl ? (loginFormControl.value as string) : undefined;
    const password = password0FormControl ? (password0FormControl.value as string) : undefined;

    const passwordConfirm = password1FormControl
      ? (password1FormControl.value as string)
      : undefined;

    this.registerGroup.disable();

    if (password !== passwordConfirm) {
      this.registerGroup.enable();
      return;
    }

    if (!email || !password) {
      password1FormControl.reset();
      this.registerGroup.enable();
      return;
    }

    this.store.dispatch(
      new ActionAuthRegister({
        email,
        password,
      })
    );
  }

  backToLoginClicked(): void {
    this.router.navigate(['/auth/login']);
  }
}
