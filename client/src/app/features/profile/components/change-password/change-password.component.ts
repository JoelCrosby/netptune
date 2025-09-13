import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
} from '@angular/core';
import {
  FormControl,
  FormGroup,
  Validators,
  FormsModule,
  ReactiveFormsModule,
} from '@angular/forms';
import { selectCurrentUserId } from '@core/auth/store/auth.selectors';
import { ChangePasswordRequest } from '@core/models/requests/change-password-request';
import { FormErrorStateMatcher } from '@core/util/forms/form-error-state-matcher';
import { Actions, ofType } from '@ngrx/effects';
import { select, Store } from '@ngrx/store';
import * as ProfileActions from '@profile/store/profile.actions';
import * as ProfileSelectors from '@profile/store/profile.selectors';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { first, shareReplay, takeUntil, tap } from 'rxjs/operators';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { MatButton } from '@angular/material/button';
import { AsyncPipe } from '@angular/common';

@Component({
  selector: 'app-change-password',
  templateUrl: './change-password.component.html',
  styleUrls: ['./change-password.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    FormInputComponent,
    MatButton,
    AsyncPipe,
  ],
})
export class ChangePasswordComponent implements OnInit, OnDestroy {
  formGroup = new FormGroup({
    currentPassword: new FormControl('', [Validators.required]),
    newPassword: new FormControl('', [Validators.required]),
    confirmPassword: new FormControl('', [Validators.required]),
  });

  loadingPasswordChange$!: Observable<boolean>;
  matcher = new FormErrorStateMatcher();
  onDestroy$ = new Subject<void>();
  errorMessage$ = new BehaviorSubject<string>('');

  get currentPassword() {
    return this.formGroup.controls.currentPassword;
  }
  get newPassword() {
    return this.formGroup.controls.newPassword;
  }
  get confirmPassword() {
    return this.formGroup?.controls.confirmPassword;
  }

  constructor(
    private store: Store,
    private actions$: Actions,
    private cd: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadingPasswordChange$ = this.store.pipe(
      takeUntil(this.onDestroy$),
      select(ProfileSelectors.selectChangePasswordLoading),
      tap((loading) =>
        loading ? this.formGroup.disable() : this.formGroup.enable()
      ),
      shareReplay()
    );

    this.actions$
      .pipe(
        takeUntil(this.onDestroy$),
        ofType(ProfileActions.changePasswordSuccess),
        tap(() => {
          this.formGroup.reset();
          this.formGroup.enable();
        })
      )
      .subscribe();

    this.actions$
      .pipe(
        takeUntil(this.onDestroy$),
        ofType(ProfileActions.changePasswordFail),
        tap(() => {
          this.formGroup.enable();
          this.errorMessage$.next('Current Password was incorrect');
        })
      )
      .subscribe();
  }

  ngOnDestroy() {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  checkPasswords(group: FormGroup) {
    const pass = group.controls.newPassword;
    const confirmPass = group.controls.confirmPassword;

    return pass.value === confirmPass.value ? null : { notSame: true };
  }

  changePasswordClicked() {
    this.formGroup.markAllAsTouched();
    this.cd.detectChanges();

    if (this.checkPasswords(this.formGroup)) {
      this.errorMessage$.next('Passwords do not match');
      return;
    }

    if (this.formGroup.invalid) {
      return;
    }

    this.store
      .select(selectCurrentUserId)
      .pipe(
        first(),
        tap((userId) => {
          if (!userId) return;

          const request: ChangePasswordRequest = {
            userId,
            currentPassword: this.currentPassword.value as string,
            newPassword: this.newPassword.value as string,
          };

          this.store.dispatch(ProfileActions.changePassword({ request }));
        })
      )
      .subscribe();
  }
}
