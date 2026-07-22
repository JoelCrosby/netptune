import { Component, inject, signal } from '@angular/core';
import {
  disabled,
  email,
  FormField,
  form,
  maxLength,
  required,
  submit,
} from '@angular/forms/signals';
import { RouterLink } from '@angular/router';
import { requestPasswordReset } from '@app/core/store/auth/auth.actions';
import { selectRequestPasswordResetLoading } from '@app/core/store/auth/auth.selectors';
import { Store } from '@ngrx/store';
import { FormErrorsComponent } from '@static/components/form-error/form-errors.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { AuthPageContainerComponent } from '../auth-page-container/auth-page-container.component';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';
import { AuthFormPanelComponent } from '../auth-form-panel/auth-form-panel.component';

@Component({
  selector: 'app-request-password-reset',
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
  template: `<app-auth-page-container>
    <app-auth-form-panel
      heading="Request Password Reset"
      [loading]="loading()"
      (submitted)="requestPasswordReset()">
      <app-form-input
        [formField]="requestForm.email"
        label="Email"
        maxLength="128"
        id="email"
        type="email"
        autocomplete="username">
        <app-form-errors [formField]="requestForm.email" />
      </app-form-input>

      <div class="mt-2 flex flex-row items-center gap-4">
        <a
          app-stroked-button
          color="primary"
          type="button"
          [routerLink]="['/auth/login']">
          Back to Log in
        </a>

        <button app-flat-button color="primary" type="submit">
          Send Password Reset Email
        </button>
      </div>
    </app-auth-form-panel>
  </app-auth-page-container> `,
})
export class RequestPasswordResetComponent {
  private store = inject(Store);

  loading = this.store.selectSignal(selectRequestPasswordResetLoading);

  requestFormModel = signal({
    email: '',
  });

  requestForm = form(this.requestFormModel, (schema) => {
    required(schema.email, { message: 'Email is required.' });
    email(schema.email, { message: 'Enter a valid email address.' });
    maxLength(schema.email, 128);
    disabled(schema, () => this.loading());
  });

  requestPasswordReset() {
    submit(this.requestForm, async () => {
      const email = this.requestForm.email().value().trim();
      this.store.dispatch(requestPasswordReset.init({ email }));
    });
  }
}
