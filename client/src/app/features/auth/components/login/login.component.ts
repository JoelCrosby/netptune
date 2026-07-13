import { Component, inject, signal } from '@angular/core';
import {
  disabled,
  email,
  form,
  FormField,
  required,
} from '@angular/forms/signals';
import { RouterLink } from '@angular/router';
import { ButtonLinkComponent } from '@app/static/components/button/button-link.component';
import { login } from '@app/core/store/auth/auth.actions';
import {
  selectLoginLoading,
  selectShowLoginError,
} from '@app/core/store/auth/auth.selectors';
import { Store } from '@ngrx/store';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { AuthPageContainerComponent } from '../auth-page-container/auth-page-container.component';
import { LoginGithubComponent } from './login-github.component';
import { LoginGoogleComponent } from './login-google.component';
import { LoginMicrosoftComponent } from './login-microsoft.component';
import { selectBuildInfo } from '@app/core/store/meta/meta.selectors';
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
  template: `<app-auth-page-container>
    <app-auth-form-panel
      showLogo
      heading="Sign in to continue"
      [loading]="loading()"
      (submitted)="login()">
      <div class="mb-6 flex h-4 w-full flex-col items-center justify-center">
        @if (showLoginError()) {
          <div
            class="text-warn w-full rounded-[0.4rem] bg-[rgba(var(--warn-rgb),0.06)] p-[0.4rem] text-center text-sm font-medium tracking-[0.25px]">
            Username or Password was incorrect
          </div>
        }
      </div>

      <app-form-input
        [formField]="loginForm.email"
        label="Email"
        maxLength="1024"
        id="email"
        type="email"
        autocomplete="username">
      </app-form-input>

      <app-form-input
        [formField]="loginForm.password"
        label="Password"
        maxLength="1024"
        id="password"
        autocomplete="current-password"
        type="password">
      </app-form-input>

      <app-turnstile (tokenGenerated)="onTurnstileResult($event)" />

      <div class="flex items-center justify-between">
        <a
          app-button-link
          color="primary"
          type="button"
          [routerLink]="['/auth/register']">
          Create Account
        </a>

        <button
          app-stroked-button
          color="primary"
          type="submit"
          class="min-w-32">
          Sign in
        </button>
      </div>

      <div class="button-container mt-[1.4rem]">
        <a
          app-button-link
          color="primary"
          [routerLink]="['/auth/request-password-reset']">
          Forgot Password?
        </a>
      </div>

      <div class="border-border my-2 border-t"></div>

      <app-login-github />
      <app-login-google />
      <app-login-microsoft />
    </app-auth-form-panel>
    <app-build-number />
  </app-auth-page-container> `,
})
export class LoginComponent {
  private store = inject(Store);

  loading = this.store.selectSignal(selectLoginLoading);
  showLoginError = this.store.selectSignal(selectShowLoginError);
  buildInfo = this.store.selectSignal(selectBuildInfo);

  loginFormModel = signal({
    email: '',
    password: '',
    turnstile: '',
  });

  loginForm = form(this.loginFormModel, (schema) => {
    required(schema.email);
    email(schema.email);
    required(schema.password);
    required(schema.turnstile);
    disabled(schema, () => this.loading());
  });

  login() {
    if (this.loginForm().invalid()) {
      this.loginForm().markAsDirty();
      return;
    }

    const email = this.loginForm.email().value();
    const password = this.loginForm.password().value();
    const turnstile = this.loginForm.turnstile().value();

    this.store.dispatch(
      login.init({
        request: {
          email,
          password,
          turnstile,
        },
      })
    );
  }

  onTurnstileResult(token: string) {
    this.loginFormModel.update((form) => ({ ...form, turnstile: token }));
  }
}
