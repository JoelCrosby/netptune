import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
} from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  Validators,
  FormControl,
} from '@angular/forms';
import { FormErrorStateMatcher } from '@core/util/forms/form-error-state-matcher';
import { selectCurrentUserId } from '@core/auth/store/auth.selectors';
import { ChangePasswordRequest } from '@core/models/requests/change-password-request';
import { Actions, ofType } from '@ngrx/effects';
import { select, Store } from '@ngrx/store';
import * as ProfileActions from '@profile/store/profile.actions';
import * as ProfileSelectors from '@profile/store/profile.selectors';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { first, shareReplay, takeUntil, tap } from 'rxjs/operators';
import { AppState } from '@core/core.state';

@Component({
  selector: 'app-change-password',
  templateUrl: './change-password.component.html',
  styleUrls: ['./change-password.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChangePasswordComponent implements OnInit, OnDestroy {
  formGroup!: FormGroup;
  loadingPasswordChange$!: Observable<boolean>;
  matcher = new FormErrorStateMatcher();
  onDestroy$ = new Subject();
  errorMessage$ = new BehaviorSubject<string>('');

  get currentPassword() {
    return this.formGroup.get('currentPassword') as FormControl;
  }
  get newPassword() {
    return this.formGroup.get('newPassword') as FormControl;
  }
  get confirmPassword() {
    return this.formGroup?.get('confirmPassword') as FormControl;
  }

  constructor(
    private store: Store<AppState>,
    private fb: FormBuilder,
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

    this.formGroup = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', Validators.required],
      confirmPassword: ['', Validators.required],
    });

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
    const pass = group.get('newPassword') as FormControl;
    const confirmPass = group.get('confirmPassword') as FormControl;

    return pass.value === confirmPass.value ? null : { notSame: true };
  }

  changePasswordClicked() {
    this.formGroup.markAllAsTouched();
    this.cd.detectChanges();

    if (!!this.checkPasswords(this.formGroup)) {
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
            currentPassword: this.currentPassword.value,
            newPassword: this.newPassword.value,
          };

          this.store.dispatch(ProfileActions.changePassword({ request }));
        })
      )
      .subscribe();
  }
}
