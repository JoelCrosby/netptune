import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
} from '@angular/core';
import {
  FormBuilder,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
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
    FormsModule,
    ReactiveFormsModule,
    MatProgressBar,
    FormInputComponent,
    MatAnchor,
    RouterLink,
    MatButton,
    MatDivider,
  ],
})
export class LoginComponent {
  private store = inject(Store);
  private fb = inject(FormBuilder);

  loading = this.store.selectSignal(selectLoginLoading);
  showLoginError = this.store.selectSignal(selectShowLoginError);

  loginGroup = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  get email() {
    return this.loginGroup.controls.email;
  }

  get password() {
    return this.loginGroup.controls.password;
  }

  constructor() {
    effect(() => {
      if (this.loading()) {
        this.loginGroup.disable();
      } else {
        this.loginGroup.enable();
      }
    });
  }

  login() {
    if (this.email.invalid || this.password.invalid) {
      this.email.markAllAsTouched();
      return;
    }

    const email = this.email.value as string;
    const password = this.password.value as string;

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
