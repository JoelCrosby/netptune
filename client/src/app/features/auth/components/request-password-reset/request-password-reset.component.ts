import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import {
  disabled,
  email,
  FormField,
  form,
  required,
} from '@angular/forms/signals';
import { RouterLink } from '@angular/router';
import { requestPasswordReset } from '@core/auth/store/auth.actions';
import { selectRequestPasswordResetLoading } from '@core/auth/store/auth.selectors';
import { Store } from '@ngrx/store';
import { FormErrorsComponent } from '@static/components/form-error/form-errors.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { AuthPageContainerComponent } from '../auth-page-container/auth-page-container.component';
import { ProgressBarComponent } from '@app/static/components/progress-bar/progress-bar.component';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';

@Component({
  selector: 'app-request-password-reset',
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
  template: `<app-auth-page-container>
    <form
      (submit)="requestPasswordReset($event)"
      class="bg-background border-border z-1 flex w-md flex-col gap-4 rounded border p-8 shadow-lg">
      <div class="auth-progress-bar">
        @if (loading()) {
          <app-progress-bar mode="indeterminate" />
        }
      </div>

      <h3 class="mb-[2.8rem] w-full text-center font-normal tracking-normal">
        Request Password Reset
      </h3>

      <app-form-input
        [formField]="requestForm.email"
        label="Email"
        maxLength="1024"
        id="email"
        type="email"
        autocomplete="username">
        <app-form-errors [formField]="requestForm.email" />
      </app-form-input>

      <div class="mt-2 flex flex-row items-center gap-4">
        <a
          app-stroked-button
          color="primary"
          type="button"
          [routerLink]="['/auth/login']">
          Back to Log in
        </a>

        <button app-flat-button color="primary" type="submit">
          Send Password Reset Email
        </button>
      </div>
    </form>
  </app-auth-page-container> `,
})
export class RequestPasswordResetComponent {
  private store = inject(Store);

  loading = this.store.selectSignal(selectRequestPasswordResetLoading);

  requestFormModel = signal({
    email: '',
  });

  requestForm = form(this.requestFormModel, (schema) => {
    required(schema.email);
    email(schema.email);
    disabled(schema, () => this.loading());
  });

  requestPasswordReset(event: Event) {
    event.preventDefault();

    if (this.requestForm().invalid()) {
      this.requestForm().markAsDirty();
      return;
    }

    const email = this.requestForm.email().value();
    this.store.dispatch(requestPasswordReset({ email }));
  }
}
