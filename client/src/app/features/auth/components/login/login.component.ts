import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { disabled, email, Field, form, required } from '@angular/forms/signals';
import { MatAnchor, MatButton } from '@angular/material/button';
import { MatDivider } from '@angular/material/divider';
import { MatProgressBar } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { login } from '@core/auth/store/auth.actions';
import {
  selectLoginLoading,
  selectShowLoginError,
} from '@core/auth/store/auth.selectors';
import { Store } from '@ngrx/store';
import { FormInputComponent } from '@static/components/form-input/form-input.component';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatProgressBar,
    FormInputComponent,
    MatAnchor,
    RouterLink,
    MatButton,
    MatDivider,
    Field,
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
