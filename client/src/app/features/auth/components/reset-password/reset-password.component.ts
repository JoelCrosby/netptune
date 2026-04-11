import {
  ChangeDetectionStrategy,
  Component,
  inject,
  linkedSignal,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  disabled,
  form,
  FormField,
  minLength,
  required,
  validate,
} from '@angular/forms/signals';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';
import { ProgressBarComponent } from '@app/static/components/progress-bar/progress-bar.component';
import { resetPassword } from '@core/auth/store/auth.actions';
import { ResetPasswordRequest } from '@core/auth/store/auth.models';
import { selectResetPasswordLoading } from '@core/auth/store/auth.selectors';
import { Store } from '@ngrx/store';
import { FormErrorsComponent } from '@static/components/form-error/form-errors.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { AuthPageContainerComponent } from '../auth-page-container/auth-page-container.component';

@Component({
  selector: 'app-reset-password',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    AuthPageContainerComponent,
    ProgressBarComponent,
    FormInputComponent,
    FormErrorsComponent,
    RouterLink,
    FlatButtonComponent,
    StrokedButtonComponent,
    FormField,
  ],
  template: `
    <app-auth-page-container>
      <form
        (submit)="resetPassword($event)"
        class="bg-background border-border z-1 flex w-md flex-col gap-4 rounded border p-8 shadow-lg">
        <div class="auth-progress-bar">
          @if (loading()) {
            <app-progress-bar mode="indeterminate" />
          }
        </div>

        <h3 class="mb-[2.8rem] w-full text-center font-normal tracking-normal">
          Reset your password
        </h3>

        <app-form-input
          [formField]="resetForm.password0"
          label="New Password"
          maxLength="1024"
          id="new-password"
          type="password"
          autocomplete="new-password">
        </app-form-input>

        <app-form-input
          [formField]="resetForm.password1"
          label="Confirm New Password"
          maxLength="1024"
          id="confirm-new-password"
          type="password"
          autocomplete="new-password">
          <app-form-errors [formField]="resetForm.password1" />
        </app-form-input>

        <div class="button-container">
          <a
            app-stroked-button
            color="primary"
            type="button"
            class="form-action-button"
            [routerLink]="['/auth/login']">
            Back to Log in
          </a>

          <button
            app-flat-button
            color="primary"
            type="submit"
            class="form-action-button">
            Reset Password
          </button>
        </div>
      </form>
    </app-auth-page-container>
  `,
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
        return {
          kind: 'noMatch',
          message: 'Passwords do not match',
        };
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
