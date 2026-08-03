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
import { LucideLock } from '@lucide/angular';
import { IconTileComponent } from '@static/components/icon-tile.component';

@Component({
  selector: 'app-change-password',
  imports: [
    FormField,
    FormInputComponent,
    IconTileComponent,
    StrokedButtonComponent,
  ],
  template: `
    <form
      class="border-border bg-card overflow-hidden rounded-lg border shadow-sm"
      (submit)="changePasswordClicked($event)">
      <header class="border-border border-b px-6 py-5">
        <div class="flex min-w-0 items-center gap-3">
          <app-icon-tile [icon]="passwordIcon" />

          <div class="min-w-0">
            <h2
              class="font-overpass text-base font-semibold"
              i18n="Heading of the change password card">
              Password
            </h2>
            <p
              class="text-muted mt-1 text-sm"
              i18n="Explains what the change password card does">
              Change the password you use to sign in.
            </p>
          </div>
        </div>
      </header>

      <div class="max-w-120 px-6 py-5">
        <app-form-input
          type="password"
          [formField]="passwordForm.currentPassword"
          autocomplete="current-password"
          i18n-label="Label of the existing password field"
          label="Current Password"></app-form-input>

        <app-form-input
          type="password"
          [formField]="passwordForm.newPassword"
          autocomplete="new-password"
          i18n-label="Label of the new password field"
          label="New Password"></app-form-input>

        <app-form-input
          type="password"
          [formField]="passwordForm.confirmPassword"
          autocomplete="new-password"
          i18n-label="Label of the password confirmation field"
          label="Confirm Password"></app-form-input>

        @if (error()) {
          <p class="text-warn mt-1 text-sm font-medium">{{ error() }}</p>
        }
      </div>

      <footer class="border-border border-t px-6 py-4">
        <button app-stroked-button type="submit" [disabled]="loading()">
          <span i18n="Button that changes the account password">
            Change Password
          </span>
        </button>
      </footer>
    </form>
  `,
})
export class ChangePasswordComponent {
  protected readonly passwordIcon = LucideLock;

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

      return {
        kind: 'passwordMismatch',
        message: $localize`:Body of a dialog or validation message:Passwords do not match.`,
      };
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
