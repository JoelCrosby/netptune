import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { resetPassword } from '@core/auth/store/auth.actions';
import { ResetPasswordRequest } from '@core/auth/store/auth.models';
import { selectResetPasswordLoading } from '@core/auth/store/auth.selectors';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { first, tap } from 'rxjs/operators';

@Component({
  selector: 'app-reset-password',
  templateUrl: './reset-password.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResetPasswordComponent {
  authLoading$: Observable<boolean>;

  request?: ResetPasswordRequest;

  formGroup = new FormGroup({
    password0: new FormControl('', [
      Validators.required,
      Validators.minLength(4),
    ]),
    password1: new FormControl('', [
      Validators.required,
      Validators.minLength(4),
    ]),
  });

  get password0() {
    return this.formGroup.controls.password0;
  }

  get password1() {
    return this.formGroup.controls.password1;
  }

  constructor(
    private activatedRoute: ActivatedRoute,
    private store: Store
  ) {
    this.activatedRoute.data
      .pipe(
        first(),
        tap((data) => {
          this.request = data.resetPassword as ResetPasswordRequest;
        })
      )
      .subscribe();

    this.authLoading$ = this.store.select(selectResetPasswordLoading).pipe(
      tap((loading) => {
        if (loading) return this.formGroup.disable();
        return this.formGroup.enable();
      })
    );
  }

  resetPassword() {
    if (!this.request) return;

    if (
      !this.password0.value ||
      this.password0.invalid ||
      this.password1.invalid
    ) {
      this.password0.markAllAsTouched();
      return;
    }

    if (this.password0.value !== this.password1.value) {
      this.password1.setErrors({ noMatch: true });
      this.password0.markAllAsTouched();
      return;
    }

    const request: ResetPasswordRequest = {
      ...this.request,
      password: this.password0.value,
    };

    this.store.dispatch(resetPassword({ request }));
  }
}
