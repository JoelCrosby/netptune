import { ChangeDetectionStrategy, Component, OnDestroy } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import * as AuthActions from '@core/auth/store/auth.actions';
import { selectRegisterLoading } from '@core/auth/store/auth.selectors';
import { Store } from '@ngrx/store';
import { Observable, Subject } from 'rxjs';
import { tap } from 'rxjs/operators';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterComponent implements OnDestroy {
  onDestroy$ = new Subject();
  authLoading$: Observable<boolean>;

  registerGroup = new FormGroup({
    firstname: new FormControl('', [
      Validators.required,
      Validators.maxLength(128),
    ]),
    lastname: new FormControl('', [
      Validators.required,
      Validators.maxLength(128),
    ]),
    email: new FormControl('', [
      Validators.required,
      Validators.email,
      Validators.maxLength(128),
    ]),
    password0: new FormControl('', [
      Validators.required,
      Validators.minLength(4),
    ]),
    password1: new FormControl('', [
      Validators.required,
      Validators.minLength(4),
    ]),
  });

  get firstname() {
    return this.registerGroup.get('firstname');
  }

  get lastname() {
    return this.registerGroup.get('lastname');
  }

  get email() {
    return this.registerGroup.get('email');
  }

  get password0() {
    return this.registerGroup.get('password0');
  }

  get password1() {
    return this.registerGroup.get('password1');
  }

  constructor(private store: Store) {
    this.authLoading$ = this.store.select(selectRegisterLoading).pipe(
      tap((loading) => {
        if (loading) return this.registerGroup.disable();
        return this.registerGroup.enable();
      })
    );
  }

  ngOnDestroy() {
    this.store.dispatch(AuthActions.clearError({ error: 'registerError' }));

    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  register() {
    const firstname: string = this.firstname.value;
    const lastname: string = this.lastname.value;
    const email: string = this.email.value;
    const password: string = this.password0.value;
    const passwordConfirm: string = this.password1.value;

    if (password !== passwordConfirm) {
      this.password1.setErrors({ noMatch: true });
      this.registerGroup.markAsDirty();
      return;
    }

    if (!email || !password) {
      this.registerGroup.markAsDirty();
      return;
    }

    this.store.dispatch(
      AuthActions.register({
        request: {
          firstname,
          lastname,
          email,
          password,
        },
      })
    );
  }
}
