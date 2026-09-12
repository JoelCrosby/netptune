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
import { RouterLink } from '@angular/router';
import { LucideMail } from '@lucide/angular';
import { AuthCommandsService } from '@core/services/auth-commands.service';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { AuthFieldComponent } from '../auth-field/auth-field.component';
import { AuthFormPanelComponent } from '../auth-form-panel/auth-form-panel.component';
import { AuthPageContainerComponent } from '../auth-page-container/auth-page-container.component';

@Component({
  selector: 'app-request-password-reset',
  imports: [
    AuthPageContainerComponent,
    AuthFormPanelComponent,
    AuthFieldComponent,
    FlatButtonComponent,
    StrokedButtonComponent,
    LucideMail,
    RouterLink,
    FormField,
  ],
  template: `
    <app-auth-page-container>
      <app-auth-form-panel
        showLogo
        i18n-eyebrow="Label above the heading of the password reset forms"
        eyebrow="Password reset"
        [heading]="heading()"
        [loading]="loading()"
        (submitted)="requestPasswordReset()">
        @if (sentTo()) {
          <span
            class="bg-primary/12 text-primary mt-2.5 mb-3.5 flex h-11 w-11 items-center justify-center rounded-[10px]"
            panelBadge
            aria-hidden="true">
            <svg lucideMail size="22"></svg>
          </span>
        }

        @if (sentTo(); as sentTo) {
          <p class="text-foreground/50 mt-1.5 text-[13px] leading-relaxed">
            <ng-container
              i18n="
                Confirms that a password reset link was sent. EMAIL is the
                address it went to@@auth.passwordReset.sentTo">
              We sent a reset link to
              <strong class="text-foreground font-bold">{{
                sentTo // i18n(ph="EMAIL")
              }}</strong
              >. Check your spam folder if it has not arrived in a few minutes.
            </ng-container>
          </p>

          <div class="mt-5.5 flex flex-col gap-2.5">
            <button
              app-stroked-button
              color="neutral"
              type="submit"
              class="h-11.5 w-full rounded-lg font-bold tracking-[.2px]">
              <span i18n="Button that sends the password reset email again">
                Resend the link
              </span>
            </button>

            <button
              app-flat-button
              color="ghost"
              type="button"
              class="text-primary hover:bg-primary/8 h-10 w-full rounded-lg text-[13px] font-bold"
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
          <p class="text-foreground/50 mt-1.5 text-[13px] leading-relaxed">
            <ng-container
              i18n="
                Explains what the password reset form
                does@@auth.passwordReset.intro">
              Enter the email on your account and we will send a link to set a
              new password.
            </ng-container>
          </p>

          <div class="mt-5.5 flex flex-col gap-4">
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

            <button
              app-flat-button
              color="primary"
              type="submit"
              class="h-11.5 w-full rounded-lg font-bold tracking-[.2px]">
              {{ submitLabel() }}
            </button>
          </div>
        }

        <p
          class="border-border/70 text-foreground/50 mt-6 border-t pt-4.5 text-[13px]">
          <span i18n="Sits before the link back to the login form">
            Remembered it?
          </span>
          <a
            class="text-primary font-semibold hover:underline"
            [routerLink]="['/auth/login']">
            <span i18n="Link back to the login form">Back to sign in</span>
          </a>
        </p>
      </app-auth-form-panel>
    </app-auth-page-container>
  `,
})
export class RequestPasswordResetComponent {
  private auth = inject(AuthCommandsService);

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
