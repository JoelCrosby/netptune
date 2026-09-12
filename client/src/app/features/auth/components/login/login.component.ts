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
import { LucideCircleAlert, LucideLock } from '@lucide/angular';
import { AuthCommandsService } from '@core/services/auth-commands.service';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { AuthFieldComponent } from '../auth-field/auth-field.component';
import { AuthFormPanelComponent } from '../auth-form-panel/auth-form-panel.component';
import { AuthPageContainerComponent } from '../auth-page-container/auth-page-container.component';
import { TurnstileComponent } from '../turnstile/turnstile.component';
import { LoginProvidersComponent } from './login-providers.component';

@Component({
  selector: 'app-login',
  imports: [
    AuthPageContainerComponent,
    AuthFormPanelComponent,
    AuthFieldComponent,
    CheckboxComponent,
    FlatButtonComponent,
    LoginProvidersComponent,
    LucideCircleAlert,
    LucideLock,
    RouterLink,
    FormField,
    TurnstileComponent,
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
        <p class="text-foreground/50 mt-1.5 text-[13px]" panelSubtitle>
          <span i18n="Sits before the link to the registration form">
            No account yet?
          </span>
          <a
            class="text-primary font-semibold hover:underline"
            [routerLink]="['/auth/register']">
            <span i18n="Link from the login form to the registration form">
              Create one
            </span>
          </a>
        </p>

        @if (showLoginError()) {
          <div
            class="text-warn bg-warn/8 mt-4.5 flex items-center gap-2.5 rounded-lg px-3 py-2.5"
            role="alert">
            <svg
              class="shrink-0"
              lucideCircleAlert
              size="16"
              aria-hidden="true"></svg>
            <span
              class="text-[13px] font-medium tracking-[.25px]"
              i18n="Error shown when login credentials are rejected">
              That email and password do not match an account.
            </span>
          </div>
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
              class="text-primary text-xs font-semibold hover:underline"
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
              [checked]="keepSignedIn()"
              (changed)="keepSignedIn.set($event)">
              <span class="text-foreground/70 text-[13px]">
                <ng-container
                  i18n="
                    Checkbox that keeps the session alive after the browser is
                    closed
                  ">
                  Keep me signed in
                </ng-container>
              </span>
            </app-checkbox>

            <span
              class="text-foreground/45 flex shrink-0 items-center gap-1.5 text-xs">
              <svg lucideLock size="13" aria-hidden="true"></svg>
              <ng-container
                i18n="
                  Notes that the sign-in form is guarded by the Cloudflare
                  Turnstile bot check. Turnstile is a product name and must not
                  be translated
                ">
                Protected by Turnstile
              </ng-container>
            </span>
          </div>

          <app-turnstile (tokenGenerated)="onTurnstileResult($event)" />

          <button
            app-flat-button
            color="primary"
            type="submit"
            class="h-11.5 w-full rounded-lg font-bold tracking-[.2px]">
            {{ submitLabel() }}
          </button>
        </div>

        <app-login-providers />

        <p class="text-foreground/45 mt-5 text-xs leading-relaxed">
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
