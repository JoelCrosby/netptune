import {
  Component,
  computed,
  inject,
  linkedSignal,
  signal,
} from '@angular/core';
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
import { LucideLock } from '@lucide/angular';
import { ResetPasswordRequest } from '@core/models/session';
import { AuthCommandsService } from '@core/services/auth-commands.service';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { HeaderPromptComponent } from '@static/components/header-prompt.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { SupportingTextComponent } from '@static/components/supporting-text.component';
import { AuthFieldComponent } from '../auth-field/auth-field.component';
import { AuthFormPanelComponent } from '../auth-form-panel/auth-form-panel.component';
import { AuthShellComponent } from '../auth-shell/auth-shell.component';
import { PasswordStrengthMeterComponent } from '../password-strength-meter/password-strength-meter.component';

const PASSWORD_MIN_LENGTH = 8;

@Component({
  selector: 'app-reset-password',
  imports: [
    AuthShellComponent,
    HeaderPromptComponent,
    StrokedButtonComponent,
    SupportingTextComponent,
    RouterLink,
    AuthFormPanelComponent,
    AuthFieldComponent,
    FlatButtonComponent,
    PasswordStrengthMeterComponent,
    FormField,
  ],
  template: `
    <app-auth-shell>
      <app-header-prompt
        shellAction
        i18n-prompt="Sits before the link back to the login form"
        prompt="Remembered it?">
        <a
          app-stroked-button
          color="neutral"
          size="small"
          [routerLink]="['/auth/login']"
          i18n="Link back to the login form">
          Sign in
        </a>
      </app-header-prompt>

      <app-auth-form-panel
        i18n-eyebrow="Label above the heading of the password reset forms"
        eyebrow="Password reset"
        i18n-heading="Heading of the form for choosing a new password"
        heading="Set a new password"
        [icon]="lockIcon"
        [loading]="loading()"
        (submitted)="resetPassword()">
        <p app-supporting-text>
          <ng-container
            i18n="
              Explains what the form for choosing a new password
              does@@auth.resetPassword.intro">
            Choose a password you have not used here before. You will be signed
            in once it is saved.
          </ng-container>
        </p>

        <app-auth-field
          revealable
          [formField]="resetForm.password0"
          i18n-label="
            Label of the new password field on the password reset form
          "
          label="New password"
          i18n-placeholder="
            Placeholder of the password field on the registration form
          "
          placeholder="At least 8 characters"
          maxLength="1024"
          id="new-password"
          type="password"
          autocomplete="new-password">
          <app-password-strength-meter
            [password]="resetForm.password0().value()" />
        </app-auth-field>

        <app-auth-field
          [formField]="resetForm.password1"
          i18n-label="
            Label of the new password confirmation field on the password reset
            form
          "
          label="Confirm new password"
          maxLength="1024"
          id="confirm-new-password"
          type="password"
          autocomplete="new-password" />

        <button app-flat-button color="primary" block type="submit">
          {{ submitLabel() }}
        </button>
      </app-auth-form-panel>
    </app-auth-shell>
  `,
})
export class ResetPasswordComponent {
  private activatedRoute = inject(ActivatedRoute);
  private auth = inject(AuthCommandsService);

  protected readonly lockIcon = LucideLock;

  loading = this.auth.resetPasswordLoading;
  routeData = toSignal(this.activatedRoute.data);

  submitLabel = computed(() => {
    if (this.loading()) {
      return $localize`:Submit button on the password reset form while saving:Saving password…`;
    }

    return $localize`:Submit button on the password reset form:Save new password`;
  });

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
    minLength(schema.password0, PASSWORD_MIN_LENGTH, {
      message: $localize`:Validation error when a new password is too short:Use at least 8 characters.`,
    });
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
