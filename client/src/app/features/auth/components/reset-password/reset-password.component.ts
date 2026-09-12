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
import { ResetPasswordRequest } from '@core/models/session';
import { AuthCommandsService } from '@core/services/auth-commands.service';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { DividerComponent } from '@static/components/divider/divider.component';
import { TextLinkComponent } from '@static/components/text-link.component';
import { AuthFieldComponent } from '../auth-field/auth-field.component';
import { AuthFormPanelComponent } from '../auth-form-panel/auth-form-panel.component';
import { AuthPageContainerComponent } from '../auth-page-container/auth-page-container.component';
import { PasswordStrengthMeterComponent } from '../password-strength-meter/password-strength-meter.component';

const PASSWORD_MIN_LENGTH = 8;

@Component({
  selector: 'app-reset-password',
  imports: [
    AuthPageContainerComponent,
    AuthFormPanelComponent,
    AuthFieldComponent,
    FlatButtonComponent,
    DividerComponent,
    PasswordStrengthMeterComponent,
    TextLinkComponent,
    RouterLink,
    FormField,
  ],
  template: `
    <app-auth-page-container>
      <app-auth-form-panel
        showLogo
        i18n-eyebrow="Label above the heading of the password reset forms"
        eyebrow="Password reset"
        i18n-heading="Heading of the form for choosing a new password"
        heading="Set a new password"
        [loading]="loading()"
        (submitted)="resetPassword()">
        <p panelSubtitle>
          <ng-container
            i18n="
              Explains what the form for choosing a new password
              does@@auth.resetPassword.intro">
            Choose a password you have not used here before. You will be signed
            in once it is saved.
          </ng-container>
        </p>

        <div class="mt-5.5 flex flex-col gap-4">
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

          <button
            app-flat-button
            color="primary"
            size="large"
            block
            type="submit">
            {{ submitLabel() }}
          </button>
        </div>

        <app-divider class="mt-6" />

        <p panelFootnote>
          <span i18n="Sits before the link back to the login form">
            Remembered it?
          </span>
          <a app-text-link [routerLink]="['/auth/login']">
            <span i18n="Link back to the login form">Back to sign in</span>
          </a>
        </p>
      </app-auth-form-panel>
    </app-auth-page-container>
  `,
})
export class ResetPasswordComponent {
  private activatedRoute = inject(ActivatedRoute);
  private auth = inject(AuthCommandsService);

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
