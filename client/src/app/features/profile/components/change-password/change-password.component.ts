import { Component, effect, inject, signal } from '@angular/core';
import {
  disabled,
  FormField,
  form,
  maxLength,
  required,
  submit,
  validate,
} from '@angular/forms/signals';
import { selectCurrentUserId } from '@app/core/store/auth/auth.selectors';
import { ChangePasswordRequest } from '@core/models/requests/change-password-request';
import { Store } from '@ngrx/store';
import { changePassword } from '@app/core/store/profile/profile.actions';
import {
  selectChangePasswordError,
  selectChangePasswordLoading,
} from '@app/core/store/profile/profile.selectors';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';

@Component({
  selector: 'app-change-password',
  imports: [FormField, FormInputComponent, StrokedButtonComponent],
  template: `<form
    class="max-w-120 px-0"
    (submit)="changePasswordClicked($event)">
    <app-form-input
      type="password"
      [formField]="passwordForm.currentPassword"
      autocomplete="current-password"
      label="Current Password">
    </app-form-input>

    <app-form-input
      type="password"
      [formField]="passwordForm.newPassword"
      autocomplete="new-password"
      label="New Password">
    </app-form-input>

    <app-form-input
      type="password"
      [formField]="passwordForm.confirmPassword"
      autocomplete="new-password"
      label="Confirm Password">
    </app-form-input>

    @if (error()) {
      <div class="text-warn my-1 mb-3 text-sm font-medium">{{ error() }}</div>
    }

    <button
      class="mt-3 ml-auto block"
      app-stroked-button
      type="submit"
      [disabled]="loading()">
      Change Password
    </button>
  </form> `,
})
export class ChangePasswordComponent {
  private store = inject(Store);

  loading = this.store.selectSignal(selectChangePasswordLoading);

  passwordFormModel = signal({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  });

  passwordForm = form(this.passwordFormModel, (schema) => {
    required(schema.currentPassword);
    required(schema.newPassword);
    required(schema.confirmPassword);
    maxLength(schema.currentPassword, 1024);
    maxLength(schema.newPassword, 1024);
    maxLength(schema.confirmPassword, 1024);
    validate(schema.confirmPassword, (context) => {
      if (context.value() === context.valueOf(schema.newPassword)) {
        return undefined;
      }

      return { kind: 'passwordMismatch', message: 'Passwords do not match.' };
    });
    disabled(schema, () => this.loading());
  });

  changePasswordError = this.store.selectSignal(selectChangePasswordError);

  error = signal('');

  constructor() {
    effect(() => {
      if (!this.loading()) {
        this.error.set('');
      }
    });

    effect(() => this.error.set(this.changePasswordError() ?? ''));
  }

  changePasswordClicked(event: Event) {
    event.preventDefault();
    const userIdSignal = this.store.selectSignal(selectCurrentUserId);
    const userId = userIdSignal();

    if (!userId) return;

    submit(this.passwordForm, async () => {
      const request: ChangePasswordRequest = {
        userId,
        currentPassword: this.passwordForm.currentPassword().value(),
        newPassword: this.passwordForm.newPassword().value(),
      };

      this.store.dispatch(changePassword.init({ request }));
    });
  }
}
