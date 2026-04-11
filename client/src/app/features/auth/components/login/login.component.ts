import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import {
  disabled,
  email,
  form,
  FormField,
  required,
} from '@angular/forms/signals';
import { RouterLink } from '@angular/router';
import { ButtonLinkComponent } from '@app/static/components/button/button-link.component';
import { ProgressBarComponent } from '@app/static/components/progress-bar/progress-bar.component';
import { login } from '@core/auth/store/auth.actions';
import {
  selectLoginLoading,
  selectShowLoginError,
} from '@core/auth/store/auth.selectors';
import { Store } from '@ngrx/store';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { AuthPageContainerComponent } from '../auth-page-container/auth-page-container.component';
import { LoginGithubComponent } from './login-github.component';

@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    AuthPageContainerComponent,
    ProgressBarComponent,
    FormInputComponent,
    RouterLink,
    ButtonLinkComponent,
    StrokedButtonComponent,
    FormField,
    LoginGithubComponent,
    ButtonLinkComponent,
  ],
  template: `<app-auth-page-container>
    <form
      (submit)="login($event)"
      class="bg-background border-border z-1 flex w-md flex-col gap-4 rounded border p-8 shadow-lg">
      <div>
        @if (loading()) {
          <app-progress-bar mode="indeterminate" />
        }
      </div>

      <img
        src="assets/apple-touch-icon.png"
        alt="Netptune Logo"
        width="72"
        height="72"
        class="mx-auto my-2" />

      <h3 class="w-full text-center font-normal tracking-normal">
        Sign in to continue
      </h3>

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
    </form>
  </app-auth-page-container> `,
})
export class LoginComponent {
  private store = inject(Store);

  loading = this.store.selectSignal(selectLoginLoading);
  showLoginError = this.store.selectSignal(selectShowLoginError);

  loginFormModel = signal({
    email: '',
    password: '',
  });

  loginForm = form(this.loginFormModel, (schema) => {
    required(schema.email);
    email(schema.email);
    required(schema.password);
    disabled(schema, () => this.loading());
  });

  login(event: Event) {
    event.preventDefault();

    if (this.loginForm().invalid()) return;

    const email = this.loginForm.email().value();
    const password = this.loginForm.password().value();

    this.store.dispatch(
      login({
        request: {
          email,
          password,
        },
      })
    );
  }
}
