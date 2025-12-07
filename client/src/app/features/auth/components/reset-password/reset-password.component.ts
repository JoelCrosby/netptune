import {
  ChangeDetectionStrategy,
  Component,
  inject,
  linkedSignal,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  customError,
  disabled,
  Field,
  form,
  minLength,
  required,
  validate,
} from '@angular/forms/signals';
import { MatAnchor, MatButton } from '@angular/material/button';
import { MatProgressBar } from '@angular/material/progress-bar';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { resetPassword } from '@core/auth/store/auth.actions';
import { ResetPasswordRequest } from '@core/auth/store/auth.models';
import { selectResetPasswordLoading } from '@core/auth/store/auth.selectors';
import { Store } from '@ngrx/store';
import { FormErrorsComponent } from '@static/components/form-error/form-errors.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';

@Component({
  selector: 'app-reset-password',
  templateUrl: './reset-password.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatProgressBar,
    FormInputComponent,
    FormErrorsComponent,
    MatAnchor,
    RouterLink,
    MatButton,
    Field,
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

  resetFormModel = signal({
    password0: '',
    password1: '',
  });

  resetForm = form(this.resetFormModel, (schema) => {
    required(schema.password0);
    required(schema.password1);
    minLength(schema.password0, 4);
    minLength(schema.password1, 4);
    disabled(schema, () => this.loading());
    validate(schema.password1, ({ value }) => {
      if (this.resetForm.password0().value() === value()) {
        return customError({
          kind: 'noMatch',
          message: 'Passwords do not match',
        });
      }

      return null;
    });
  });

  resetPassword(event: Event) {
    event.preventDefault();

    if (!this.request || this.resetForm().invalid()) return;

    const password = this.resetForm.password0().value();

    const request: ResetPasswordRequest = {
      ...this.request(),
      password,
    };

    this.store.dispatch(resetPassword({ request }));
  }
}
