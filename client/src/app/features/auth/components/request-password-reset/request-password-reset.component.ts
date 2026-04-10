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
import { MatAnchor, MatButton } from '@angular/material/button';
import { RouterLink } from '@angular/router';
import { requestPasswordReset } from '@core/auth/store/auth.actions';
import { selectRequestPasswordResetLoading } from '@core/auth/store/auth.selectors';
import { Store } from '@ngrx/store';
import { FormErrorsComponent } from '@static/components/form-error/form-errors.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { AuthPageContainerComponent } from '../auth-page-container/auth-page-container.component';
import { ProgressBarComponent } from '@app/static/components/progress-bar/progress-bar.component';

@Component({
  selector: 'app-request-password-reset',
  templateUrl: './request-password-reset.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    AuthPageContainerComponent,
    ProgressBarComponent,
    FormInputComponent,
    FormErrorsComponent,
    MatAnchor,
    RouterLink,
    MatButton,
    FormField,
  ],
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
