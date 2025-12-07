import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { disabled, email, Field, form, required } from '@angular/forms/signals';
import { MatAnchor, MatButton } from '@angular/material/button';
import { MatProgressBar } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { requestPasswordReset } from '@core/auth/store/auth.actions';
import { selectRequestPasswordResetLoading } from '@core/auth/store/auth.selectors';
import { Store } from '@ngrx/store';
import { FormErrorsComponent } from '@static/components/form-error/form-errors.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';

@Component({
  selector: 'app-request-password-reset',
  templateUrl: './request-password-reset.component.html',
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

  requestPasswordReset() {
    if (this.requestForm().invalid()) {
      this.requestForm().markAsDirty();
      return;
    }

    const email = this.requestForm.email().value();
    this.store.dispatch(requestPasswordReset({ email }));
  }
}
