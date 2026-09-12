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
import { LucideCircleAlert } from '@lucide/angular';
import { AuthCommandsService } from '@core/services/auth-commands.service';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { TextLinkComponent } from '@static/components/text-link.component';
import { CalloutComponent } from '@static/components/callout/callout.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { AuthFieldComponent } from '../auth-field/auth-field.component';
import { AuthFormPanelComponent } from '../auth-form-panel/auth-form-panel.component';
import { AuthPageContainerComponent } from '../auth-page-container/auth-page-container.component';
import { TurnstileComponent } from '../turnstile/turnstile.component';
import { TurnstileNoticeComponent } from '../turnstile/turnstile-notice.component';
import { LoginProvidersComponent } from './login-providers.component';

@Component({
  selector: 'app-login',
  imports: [
    AuthPageContainerComponent,
    AuthFormPanelComponent,
    AuthFieldComponent,
    CalloutComponent,
    CheckboxComponent,
    FlatButtonComponent,
    LoginProvidersComponent,
    TextLinkComponent,
    RouterLink,
    FormField,
    TurnstileComponent,
    TurnstileNoticeComponent,
  ],
  template: `
    <app-auth-page-container>
      <app-auth-form-panel
        showLogo
        i18n-eyebrow="Label above the heading of the sign-in form"
        eyebrow="Netptune account"
        i18n-heading="Heading of the login form"
        heading="Sign in to continue"
        [loading]="loading()"
        (submitted)="login()">
        <p panelSubtitle>
          <span i18n="Sits before the link to the registration form">
            No account yet?
          </span>
          <a app-text-link [routerLink]="['/auth/register']">
            <span i18n="Link from the login form to the registration form">
              Create one
            </span>
          </a>
        </p>

        @if (showLoginError()) {
          <app-callout
            class="mt-4.5"
            color="warn"
            role="alert"
            [icon]="alertIcon">
            <span i18n="Error shown when login credentials are rejected">
              That email and password do not match an account.
            </span>
          </app-callout>
        }

        <div class="mt-5.5 flex flex-col gap-4">
          <app-auth-field
            [formField]="loginForm.email"
            i18n-label="Label of the e-mail address field on the login form"
            label="Email"
            i18n-placeholder="
              Placeholder of the e-mail address field on auth forms
            "
            placeholder="you@company.com"
            maxLength="128"
            id="email"
            type="email"
            autocomplete="username" />

          <app-auth-field
            revealable
            [formField]="loginForm.password"
            i18n-label="Label of the password field on the login form"
            label="Password"
            maxLength="1024"
            id="password"
            type="password"
            autocomplete="current-password">
            <a
              app-text-link
              size="small"
              fieldAction
              [routerLink]="['/auth/request-password-reset']">
              <span
                i18n="
                  Link from the login form to the password reset request form
                ">
                Forgot password?
              </span>
            </a>
          </app-auth-field>

          <div class="flex items-center justify-between gap-4">
            <app-checkbox
              density="compact"
              [checked]="keepSignedIn()"
              (changed)="keepSignedIn.set($event)">
              <ng-container
                i18n="
                  Checkbox that keeps the session alive after the browser is
                  closed
                ">
                Keep me signed in
              </ng-container>
            </app-checkbox>

            <app-turnstile-notice />
          </div>

          <app-turnstile (tokenGenerated)="onTurnstileResult($event)" />

          <button
            app-flat-button
            color="primary"
            size="large"
            block
            type="submit">
            {{ submitLabel() }}
          </button>
        </div>

        <app-login-providers />

        <p panelFootnote>
          <ng-container
            i18n="
              Legal note in the footer of the sign-in card@@auth.login.legalNote">
            By signing in you agree to the terms and privacy policy.
          </ng-container>
        </p>
      </app-auth-form-panel>
    </app-auth-page-container>
  `,
})
export class LoginComponent {
  private auth = inject(AuthCommandsService);

  protected readonly alertIcon = LucideCircleAlert;

  loading = this.auth.loginLoading;
  showLoginError = this.auth.loginError;

  keepSignedIn = signal(true);

  submitLabel = computed(() => {
    if (this.loading()) {
      return $localize`:Submit button on the login form while signing in:Signing in…`;
    }

    return $localize`:Submit button on the login form:Sign in`;
  });

  loginFormModel = signal({
    email: '',
    password: '',
    turnstile: '',
  });

  loginForm = form(this.loginFormModel, (schema) => {
    required(schema.email, {
      message: $localize`:Validation error when the e-mail field is empty:Email is required.`,
    });
    email(schema.email, {
      message: $localize`:Validation error when the e-mail field is not a valid address:Enter a valid email address.`,
    });
    maxLength(schema.email, 128);
    required(schema.password, {
      message: $localize`:Validation error when the password field is empty:Password is required.`,
    });
    maxLength(schema.password, 1024);
    required(schema.turnstile);
    disabled(schema, () => this.loading());
  });

  login() {
    submit(this.loginForm, async () => {
      const email = this.loginForm.email().value().trim();
      const password = this.loginForm.password().value();
      const turnstile = this.loginForm.turnstile().value();

      this.auth.login({
        email,
        password,
        turnstile,
        keepSignedIn: this.keepSignedIn(),
      });
    });
  }

  onTurnstileResult(token: string) {
    this.loginFormModel.update((form) => ({ ...form, turnstile: token }));
  }
}
