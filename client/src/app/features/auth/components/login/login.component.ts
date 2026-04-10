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
  templateUrl: './login.component.html',
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

  onGithubSignInClicked() {
    location.href = '/api/auth/github-login';
  }
}
