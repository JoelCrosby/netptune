import { Component, inject, linkedSignal, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  disabled,
  form,
  FormField,
  maxLength,
  minLength,
  required,
  submit,
  validate,
} from '@angular/forms/signals';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';
import { ResetPasswordRequest } from '@core/models/session';
import { AuthCommandsService } from '@core/services/auth-commands.service';
import { FormErrorsComponent } from '@static/components/form-error/form-errors.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { AuthPageContainerComponent } from '../auth-page-container/auth-page-container.component';
import { AuthFormPanelComponent } from '../auth-form-panel/auth-form-panel.component';

@Component({
  selector: 'app-reset-password',
  imports: [
    AuthPageContainerComponent,
    AuthFormPanelComponent,
    FormInputComponent,
    FormErrorsComponent,
    RouterLink,
    FlatButtonComponent,
    StrokedButtonComponent,
    FormField,
  ],
  template: `
    <app-auth-page-container>
      <app-auth-form-panel
        i18n-heading="Heading of the form for choosing a new password"
        heading="Reset your password"
        [loading]="loading()"
        (submitted)="resetPassword()">
        <app-form-input
          [formField]="resetForm.password0"
          i18n-label="
            Label of the new password field on the password reset form
          "
          label="New Password"
          maxLength="1024"
          id="new-password"
          type="password"
          autocomplete="new-password"></app-form-input>

        <app-form-input
          [formField]="resetForm.password1"
          i18n-label="
            Label of the new password confirmation field on the password reset
            form
          "
          label="Confirm New Password"
          maxLength="1024"
          id="confirm-new-password"
          type="password"
          autocomplete="new-password">
          <app-form-errors [formField]="resetForm.password1" />
        </app-form-input>

        <div class="button-container">
          <a
            app-stroked-button
            color="primary"
            type="button"
            class="form-action-button"
            [routerLink]="['/auth/login']">
            <span i18n="Link from the registration form back to the login form">
              Back to Log in
            </span>
          </a>

          <button
            app-flat-button
            color="primary"
            type="submit"
            class="form-action-button">
            <span i18n="Submit button on the password reset form">
              Reset Password
            </span>
          </button>
        </div>
      </app-auth-form-panel>
    </app-auth-page-container>
  `,
})
export class ResetPasswordComponent {
  private activatedRoute = inject(ActivatedRoute);
  private auth = inject(AuthCommandsService);

  loading = this.auth.resetPasswordLoading;
  routeData = toSignal(this.activatedRoute.data);

  request = linkedSignal<ResetPasswordRequest>(() => {
    return this.routeData()?.resetPassword;
  });

  resetFormModel = signal({
    password0: '',
    password1: '',
  });

  resetForm = form(this.resetFormModel, (schema) => {
    required(schema.password0, {
      message: $localize`:Validation error when the password field is empty:Password is required.`,
    });
    required(schema.password1, {
      message: $localize`:Validation error when the password confirmation field is empty:Confirm your password.`,
    });
    minLength(schema.password0, 4);
    minLength(schema.password1, 4);
    maxLength(schema.password0, 1024);
    maxLength(schema.password1, 1024);
    disabled(schema, () => this.loading());
    validate(schema.password1, (context) => {
      if (context.valueOf(schema.password0) !== context.value()) {
        return {
          kind: 'noMatch',
          message: $localize`:Validation error when the two password fields differ:Passwords do not match`,
        };
      }

      return undefined;
    });
  });

  resetPassword() {
    submit(this.resetForm, async () => {
      const password = this.resetForm.password0().value();
      const request: ResetPasswordRequest = {
        ...this.request(),
        password,
      };

      this.auth.resetPassword(request);
    });
  }
}
