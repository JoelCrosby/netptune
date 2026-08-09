import { Component, inject, signal } from '@angular/core';
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
import { ButtonLinkComponent } from '@app/static/components/button/button-link.component';
import { AuthCommandsService } from '@core/services/auth-commands.service';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { AuthPageContainerComponent } from '../auth-page-container/auth-page-container.component';
import { LoginGithubComponent } from './login-github.component';
import { LoginGoogleComponent } from './login-google.component';
import { LoginMicrosoftComponent } from './login-microsoft.component';
import { BuildNumberComponent } from '@app/static/components/build-number/build-number.component';
import { TurnstileComponent } from '../turnstile/turnstile.component';
import { AuthFormPanelComponent } from '../auth-form-panel/auth-form-panel.component';

@Component({
  selector: 'app-login',
  imports: [
    AuthPageContainerComponent,
    AuthFormPanelComponent,
    FormInputComponent,
    RouterLink,
    ButtonLinkComponent,
    StrokedButtonComponent,
    FormField,
    LoginGithubComponent,
    LoginGoogleComponent,
    LoginMicrosoftComponent,
    ButtonLinkComponent,
    BuildNumberComponent,
    TurnstileComponent,
  ],
  template: `
    <app-auth-page-container>
      <app-auth-form-panel
        showLogo
        i18n-heading="Heading of the login form"
        heading="Sign in to continue"
        [loading]="loading()"
        (submitted)="login()">
        <div class="mb-6 flex h-4 w-full flex-col items-center justify-center">
          @if (showLoginError()) {
            <div
              class="text-warn w-full rounded-[0.4rem] bg-[rgba(var(--warn-rgb),0.06)] p-[0.4rem] text-center text-sm font-medium tracking-[0.25px]">
              <span i18n="Error shown when login credentials are rejected">
                Username or Password was incorrect
              </span>
            </div>
          }
        </div>

        <app-form-input
          [formField]="loginForm.email"
          i18n-label="Label of the e-mail address field on the login form"
          label="Email"
          maxLength="128"
          id="email"
          type="email"
          autocomplete="username"></app-form-input>

        <app-form-input
          [formField]="loginForm.password"
          i18n-label="Label of the password field on the login form"
          label="Password"
          maxLength="1024"
          id="password"
          autocomplete="current-password"
          type="password"></app-form-input>

        <app-turnstile (tokenGenerated)="onTurnstileResult($event)" />

        <div class="flex items-center justify-between">
          <a
            app-button-link
            color="primary"
            type="button"
            [routerLink]="['/auth/register']">
            <span i18n="Link from the login form to the registration form">
              Create Account
            </span>
          </a>

          <button
            app-stroked-button
            color="primary"
            type="submit"
            class="border-linear-to-tl min-w-32 border-4 via-fuchsia-300/30 to-sky-300/30">
            <span i18n="Submit button on the login form">Sign in</span>
          </button>
        </div>

        <div class="button-container mt-[1.4rem]">
          <a
            app-button-link
            color="primary"
            [routerLink]="['/auth/request-password-reset']">
            <span
              i18n="
                Link from the login form to the password reset request form
              ">
              Forgot Password?
            </span>
          </a>
        </div>

        <div class="border-border my-2 border-t"></div>

        <app-login-github />
        <app-login-google />
        <app-login-microsoft />
      </app-auth-form-panel>
      <app-build-number />
    </app-auth-page-container>
  `,
})
export class LoginComponent {
  private auth = inject(AuthCommandsService);

  loading = this.auth.loginLoading;
  showLoginError = this.auth.loginError;

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

      this.auth.login({ email, password, turnstile });
    });
  }

  onTurnstileResult(token: string) {
    this.loginFormModel.update((form) => ({ ...form, turnstile: token }));
  }
}
