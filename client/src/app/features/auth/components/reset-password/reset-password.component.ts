import { Component, inject, linkedSignal, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  disabled,
  form,
  FormField,
  minLength,
  required,
  validate,
} from '@angular/forms/signals';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';
import { resetPassword } from '@app/core/store/auth/auth.actions';
import { ResetPasswordRequest } from '@app/core/store/auth/auth.models';
import { selectResetPasswordLoading } from '@app/core/store/auth/auth.selectors';
import { Store } from '@ngrx/store';
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
        heading="Reset your password"
        [loading]="loading()"
        (submitted)="resetPassword()">
        <app-form-input
          [formField]="resetForm.password0"
          label="New Password"
          maxLength="1024"
          id="new-password"
          type="password"
          autocomplete="new-password">
        </app-form-input>

        <app-form-input
          [formField]="resetForm.password1"
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
            Back to Log in
          </a>

          <button
            app-flat-button
            color="primary"
            type="submit"
            class="form-action-button">
            Reset Password
          </button>
        </div>
      </app-auth-form-panel>
    </app-auth-page-container>
  `,
})
export class ResetPasswordComponent {
  private activatedRoute = inject(ActivatedRoute);
  private store = inject(Store);

  loading = this.store.selectSignal(selectResetPasswordLoading);
  routeData = toSignal(this.activatedRoute.data);

  request = linkedSignal<ResetPasswordRequest>(() => {
    return this.routeData()?.resetPassword;
  });

  resetFormModel = signal({
    password0: '',
    password1: '',
  });

  resetForm = form(this.resetFormModel, (schema) => {
    required(schema.password0);
    required(schema.password1);
    minLength(schema.password0, 4);
    minLength(schema.password1, 4);
    disabled(schema, () => this.loading());
    validate(schema.password1, ({ value }) => {
      if (this.resetForm.password0().value() === value()) {
        return {
          kind: 'noMatch',
          message: 'Passwords do not match',
        };
      }

      return null;
    });
  });

  resetPassword() {
    if (!this.request || this.resetForm().invalid()) return;

    const password = this.resetForm.password0().value();

    const request: ResetPasswordRequest = {
      ...this.request(),
      password,
    };

    this.store.dispatch(resetPassword.init({ request }));
  }
}
