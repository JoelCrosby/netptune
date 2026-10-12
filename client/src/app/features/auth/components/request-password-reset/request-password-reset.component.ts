import { RouterLink } from '@angular/router';
import { Component, computed, inject, signal } from '@angular/core';
import {
  disabled,
  email,
  form,
  FormField,
  maxLength,
  required,
  submit,
} from '@angular/forms/signals';
import { LucideKeyRound, LucideMail } from '@lucide/angular';
import { AuthCommandsService } from '@core/services/auth-commands.service';
import { ButtonLinkComponent } from '@static/components/button/button-link.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { HeaderPromptComponent } from '@static/components/header-prompt.component';
import { SupportingTextComponent } from '@static/components/supporting-text.component';
import { AuthFieldComponent } from '../auth-field/auth-field.component';
import { AuthFormPanelComponent } from '../auth-form-panel/auth-form-panel.component';
import { AuthShellComponent } from '../auth-shell/auth-shell.component';

@Component({
  selector: 'app-request-password-reset',
  imports: [
    AuthShellComponent,
    HeaderPromptComponent,
    SupportingTextComponent,
    RouterLink,
    AuthFormPanelComponent,
    AuthFieldComponent,
    ButtonLinkComponent,
    FlatButtonComponent,
    StrokedButtonComponent,
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
        [heading]="heading()"
        [icon]="sentTo() ? mailIcon : keyIcon"
        [loading]="loading()"
        (submitted)="requestPasswordReset()">
        @if (sentTo(); as sentTo) {
          <p app-supporting-text>
            <ng-container
              i18n="
                Confirms that a password reset link was sent. EMAIL is the
                address it went to@@auth.passwordReset.sentTo">
              We sent a reset link to
              <strong>{{
                sentTo // i18n(ph="EMAIL")
              }}</strong
              >. Check your spam folder if it has not arrived in a few minutes.
            </ng-container>
          </p>

          <div class="flex flex-col gap-2">
            <button app-stroked-button color="neutral" block type="submit">
              <span i18n="Button that sends the password reset email again">
                Resend the link
              </span>
            </button>

            <button
              app-button-link
              color="primary"
              block
              type="button"
              (click)="useDifferentEmail()">
              <span
                i18n="
                  Button that returns to the form for entering another email
                  address
                ">
                Use a different email
              </span>
            </button>
          </div>
        } @else {
          <p app-supporting-text>
            <ng-container
              i18n="
                Explains what the password reset form
                does@@auth.passwordReset.intro">
              Enter the email on your account and we will send a link to set a
              new password.
            </ng-container>
          </p>

          <app-auth-field
            [formField]="requestForm.email"
            i18n-label="
              Label of the e-mail address field on the password reset request
              form
            "
            label="Email"
            i18n-placeholder="
              Placeholder of the e-mail address field on auth forms
            "
            placeholder="you@company.com"
            maxLength="128"
            id="email"
            type="email"
            autocomplete="username" />

          <button app-flat-button color="primary" block type="submit">
            {{ submitLabel() }}
          </button>
        }
      </app-auth-form-panel>
    </app-auth-shell>
  `,
})
export class RequestPasswordResetComponent {
  private auth = inject(AuthCommandsService);

  protected readonly mailIcon = LucideMail;
  protected readonly keyIcon = LucideKeyRound;

  loading = this.auth.requestPasswordResetLoading;
  sentTo = this.auth.passwordResetEmail;

  heading = computed(() => {
    if (this.sentTo()) {
      return $localize`:Heading shown once the password reset email has been sent:Check your inbox`;
    }

    return $localize`:Heading of the password reset request form:Forgot your password?`;
  });

  submitLabel = computed(() => {
    if (this.loading()) {
      return $localize`:Submit button on the password reset request form while sending:Sending link…`;
    }

    return $localize`:Submit button on the password reset request form:Send reset link`;
  });

  requestFormModel = signal({
    email: '',
  });

  requestForm = form(this.requestFormModel, (schema) => {
    required(schema.email, {
      message: $localize`:Validation error when the e-mail field is empty:Email is required.`,
    });
    email(schema.email, {
      message: $localize`:Validation error when the e-mail field is not a valid address:Enter a valid email address.`,
    });
    maxLength(schema.email, 128);
    disabled(schema, () => this.loading());
  });

  requestPasswordReset() {
    submit(this.requestForm, async () => {
      const email = this.requestForm.email().value().trim();
      this.auth.requestPasswordReset(email);
    });
  }

  useDifferentEmail() {
    this.auth.clearPasswordResetSent();
  }
}
