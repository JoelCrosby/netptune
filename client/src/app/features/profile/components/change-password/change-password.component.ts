import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  signal,
} from '@angular/core';
import { disabled, Field, form, required } from '@angular/forms/signals';
import { MatButton } from '@angular/material/button';
import { selectCurrentUserId } from '@core/auth/store/auth.selectors';
import { ChangePasswordRequest } from '@core/models/requests/change-password-request';
import { Store } from '@ngrx/store';
import { changePassword } from '@profile/store/profile.actions';
import {
  selectChangePasswordError,
  selectChangePasswordLoading,
} from '@profile/store/profile.selectors';
import { FormInputComponent } from '@static/components/form-input/form-input.component';

@Component({
  selector: 'app-change-password',
  templateUrl: './change-password.component.html',
  styleUrls: ['./change-password.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Field, FormInputComponent, MatButton],
})
export class ChangePasswordComponent {
  private store = inject(Store);

  loading = this.store.selectSignal(selectChangePasswordLoading);

  passwordFormModel = signal({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  });

  passwordForm = form(this.passwordFormModel, (schema) => {
    required(schema.currentPassword);
    required(schema.newPassword);
    required(schema.confirmPassword);
    disabled(schema, () => this.loading());
  });

  changePasswordError = this.store.selectSignal(selectChangePasswordError);

  error = signal('');

  constructor() {
    effect(() => {
      if (!this.loading()) {
        this.error.set('');
      }
    });

    effect(() => {
      if (
        this.passwordForm().touched() &&
        this.passwordForm.confirmPassword().value()
      ) {
        this.passwordsMatch();
      }
    });

    effect(() => this.error.set(this.changePasswordError() ?? ''));
  }

  passwordsMatch() {
    const pass = this.passwordForm.newPassword().value();
    const confirmPass = this.passwordForm.confirmPassword().value();

    if (pass === confirmPass) {
      this.error.set('');
      return false;
    } else {
      this.error.set('Passwords do not match.');
      return true;
    }
  }

  changePasswordClicked() {
    if (this.passwordsMatch()) {
      return;
    }

    if (this.passwordForm().invalid()) {
      return;
    }

    const userIdSignal = this.store.selectSignal(selectCurrentUserId);
    const userId = userIdSignal();

    if (!userId) return;

    const request: ChangePasswordRequest = {
      userId,
      currentPassword: this.passwordForm.currentPassword().value(),
      newPassword: this.passwordForm.newPassword().value(),
    };

    this.store.dispatch(changePassword({ request }));
  }
}
