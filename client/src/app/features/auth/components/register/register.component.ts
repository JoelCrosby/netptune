import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
} from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import * as AuthActions from '@core/auth/store/auth.actions';
import { WorkspaceInvite } from '@core/auth/store/auth.models';
import { selectRegisterLoading } from '@core/auth/store/auth.selectors';
import { Store } from '@ngrx/store';
import { Observable, Subject } from 'rxjs';
import { first, map, takeUntil, tap } from 'rxjs/operators';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterComponent implements OnInit, OnDestroy {
  authLoading$: Observable<boolean>;
  request$: Observable<WorkspaceInvite>;

  onDestroy$ = new Subject();

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

  constructor(private store: Store, private activatedRoute: ActivatedRoute) {
    this.authLoading$ = this.store.select(selectRegisterLoading).pipe(
      tap((loading) => {
        if (loading) return this.registerGroup.disable();
        return this.registerGroup.enable();
      })
    );
  }

  ngOnInit() {
    this.request$ = this.activatedRoute.data.pipe(
      map((data) => {
        const invite = data.invite as WorkspaceInvite;
        return invite.success ? invite : null;
      }),
      tap((invite) => {
        console.log({ invite });

        if (invite) {
          this.email.setValue(invite.email, { emitEvent: false });
          this.email.disable({ emitEvent: false });
        }
      })
    );

    this.request$.pipe(takeUntil(this.onDestroy$)).subscribe();
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

    this.request$
      .pipe(
        first(),
        map((invite) => invite?.code),
        tap((inviteCode) => {
          this.store.dispatch(
            AuthActions.register({
              request: {
                firstname,
                lastname,
                email,
                password,
                inviteCode,
              },
            })
          );
        })
      )
      .subscribe();
  }
}
