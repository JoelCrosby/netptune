import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  effect,
  inject,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { selectCurrentUserId } from '@core/auth/store/auth.selectors';
import { ChangePasswordRequest } from '@core/models/requests/change-password-request';
import { Store } from '@ngrx/store';
import { changePassword } from '@profile/store/profile.actions';
import {
  selectChangePasswordError,
  selectChangePasswordLoading,
} from '@profile/store/profile.selectors';
import { FormInputComponent } from '@static/components/form-input/form-input.component';

@Component({
  selector: 'app-change-password',
  templateUrl: './change-password.component.html',
  styleUrls: ['./change-password.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ReactiveFormsModule, FormInputComponent, MatButton],
})
export class ChangePasswordComponent {
  private store = inject(Store);
  private fb = inject(FormBuilder);
  private cd = inject(ChangeDetectorRef);

  formGroup = this.fb.nonNullable.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required]],
    confirmPassword: ['', [Validators.required]],
  });

  get currentPassword() {
    return this.formGroup.controls.currentPassword;
  }
  get newPassword() {
    return this.formGroup.controls.newPassword;
  }
  get confirmPassword() {
    return this.formGroup?.controls.confirmPassword;
  }

  confirmPasswordChanges = toSignal(this.confirmPassword.valueChanges);
  changePasswordError = this.store.selectSignal(selectChangePasswordError);

  error = signal('');

  loading = this.store.selectSignal(selectChangePasswordLoading);

  constructor() {
    effect(() => {
      this.loading() ? this.formGroup.disable() : this.formGroup.enable();

      if (!this.loading()) {
        this.formGroup.reset();
        this.formGroup.enable();
        this.error.set('');
      }
    });

    effect(() => {
      console.log('confirmPasswordChanges: ', this.confirmPasswordChanges());

      if (this.confirmPasswordChanges()) {
        this.passwordsMatch(this.formGroup);
      }
    });

    effect(() => this.error.set(this.changePasswordError() ?? ''));
  }

  passwordsMatch(group: FormGroup) {
    const pass = group.controls.newPassword;
    const confirmPass = group.controls.confirmPassword;

    if (pass.value === confirmPass.value) {
      this.error.set('');
      return false;
    } else {
      this.error.set('Passwords do not match.');
      return true;
    }
  }

  changePasswordClicked() {
    this.formGroup.markAllAsTouched();
    this.cd.detectChanges();

    if (this.passwordsMatch(this.formGroup)) {
      return;
    }

    if (this.formGroup.invalid) {
      return;
    }

    const userIdSignal = this.store.selectSignal(selectCurrentUserId);
    const userId = userIdSignal();

    if (!userId) return;

    const request: ChangePasswordRequest = {
      userId,
      currentPassword: this.currentPassword.value as string,
      newPassword: this.newPassword.value as string,
    };

    this.store.dispatch(changePassword({ request }));
  }
}
