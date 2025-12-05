import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  linkedSignal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  FormControl,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatAnchor, MatButton } from '@angular/material/button';
import { MatProgressBar } from '@angular/material/progress-bar';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { resetPassword } from '@core/auth/store/auth.actions';
import { ResetPasswordRequest } from '@core/auth/store/auth.models';
import { selectResetPasswordLoading } from '@core/auth/store/auth.selectors';
import { Store } from '@ngrx/store';
import { FormErrorComponent } from '@static/components/form-error/form-error.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';

@Component({
  selector: 'app-reset-password',
  templateUrl: './reset-password.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    MatProgressBar,
    FormInputComponent,
    FormErrorComponent,
    MatAnchor,
    RouterLink,
    MatButton,
  ],
})
export class ResetPasswordComponent {
  private activatedRoute = inject(ActivatedRoute);
  private store = inject(Store);

  loading = this.store.selectSignal(selectResetPasswordLoading);
  routeData = toSignal(this.activatedRoute.data);

  request = linkedSignal<ResetPasswordRequest>(() => {
    return this.routeData()?.resetPassword;
  });

  form = new FormGroup({
    password0: new FormControl('', [
      Validators.required,
      Validators.minLength(4),
    ]),
    password1: new FormControl('', [
      Validators.required,
      Validators.minLength(4),
    ]),
  });

  get password0() {
    return this.form.controls.password0;
  }

  get password1() {
    return this.form.controls.password1;
  }

  constructor() {
    effect(() => {
      if (this.loading()) {
        this.form.disable();
      } else {
        this.form.enable();
      }
    });
  }

  resetPassword() {
    if (!this.request) return;

    if (
      !this.password0.value ||
      this.password0.invalid ||
      this.password1.invalid
    ) {
      this.password0.markAllAsTouched();
      return;
    }

    if (this.password0.value !== this.password1.value) {
      this.password1.setErrors({ noMatch: true });
      this.password0.markAllAsTouched();
      return;
    }

    const request: ResetPasswordRequest = {
      ...this.request(),
      password: this.password0.value,
    };

    this.store.dispatch(resetPassword({ request }));
  }
}
