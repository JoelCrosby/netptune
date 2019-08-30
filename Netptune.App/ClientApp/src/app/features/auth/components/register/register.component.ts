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
    firstname: new FormControl('', [Validators.required, Validators.maxLength(128)]),
    lastname: new FormControl('', [Validators.required, Validators.maxLength(128)]),
    email: new FormControl('', [Validators.required, Validators.email, Validators.maxLength(128)]),
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
    const firstname: string = this.registerGroup.get('firstname').value;
    const lastname: string = this.registerGroup.get('lastname').value;
    const email: string = this.registerGroup.get('email').value;
    const password: string = this.registerGroup.get('password0').value;
    const passwordConfirm: string = this.registerGroup.get('password1').value;

    this.registerGroup.disable();

    if (password !== passwordConfirm) {
      this.registerGroup.enable();
      return;
    }

    if (!email || !password) {
      this.registerGroup.enable();
      return;
    }

    this.store.dispatch(
      new ActionAuthRegister({
        request: {
          firstname,
          lastname,
          email,
          password,
        },
      })
    );
  }

  backToLoginClicked(): void {
    this.router.navigate(['/auth/login']);
  }
}
