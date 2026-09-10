import { Component, computed, effect, inject, signal } from '@angular/core';
import { SessionService } from '@core/services/session.service';
import {
  disabled,
  FormField,
  form,
  maxLength,
  required,
  submit,
  validate,
} from '@angular/forms/signals';
import { ChangePasswordRequest } from '@core/models/requests/change-password-request';
import { loginMethodsResource } from '@core/resources/profile.resource';
import { ProfileCommandsService } from '@core/services/profile-commands.service';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { LucideLock } from '@lucide/angular';
import { PanelComponent } from '@static/components/panel.component';
import { PanelBodyComponent } from '@static/components/panel-body.component';
import { PanelFooterComponent } from '@static/components/panel-footer.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';

@Component({
  selector: 'app-account-password',
  imports: [
    FormField,
    FormInputComponent,
    PanelBodyComponent,
    PanelComponent,
    PanelFooterComponent,
    PanelHeaderComponent,
    StrokedButtonComponent,
  ],
  host: { class: 'block' },
  template: `
    @if (hasPassword() !== undefined) {
      <form app-panel surface="card" (submit)="submitClicked($event)">
        <app-panel-header
          density="comfortable"
          [icon]="passwordIcon"
          i18n-heading="Heading of the account password card"
          heading="Password">
          <div panelHeading>
            @if (hasPassword()) {
              <p
                class="text-muted mt-1 text-sm"
                i18n="Explains what the change password card does">
                Change the password you use to sign in.
              </p>
            } @else {
              <p
                class="text-muted mt-1 text-sm"
                i18n="
                  Explains what the set password card does for accounts created
                  through an external provider
                ">
                You sign in with a linked account. Set a password to also sign
                in with your email address.
              </p>
            }
          </div>
        </app-panel-header>

        <app-panel-body class="max-w-120">
          @if (hasPassword()) {
            <app-form-input
              type="password"
              [formField]="passwordForm.currentPassword"
              autocomplete="current-password"
              i18n-label="Label of the existing password field"
              label="Current Password"></app-form-input>
          }

          <app-form-input
            type="password"
            [formField]="passwordForm.newPassword"
            autocomplete="new-password"
            [label]="newPasswordLabel()"></app-form-input>

          <app-form-input
            type="password"
            [formField]="passwordForm.confirmPassword"
            autocomplete="new-password"
            i18n-label="Label of the password confirmation field"
            label="Confirm Password"></app-form-input>

          @if (error()) {
            <p class="text-warn mt-1 text-sm font-medium">{{ error() }}</p>
          }
        </app-panel-body>

        <app-panel-footer>
          <button app-stroked-button type="submit" [disabled]="loading()">
            @if (hasPassword()) {
              <span i18n="Button that changes the account password">
                Change Password
              </span>
            } @else {
              <span i18n="Button that sets a password on the account">
                Set Password
              </span>
            }
          </button>
        </app-panel-footer>
      </form>
    }
  `,
})
export class AccountPasswordComponent {
  protected readonly passwordIcon = LucideLock;

  // Undefined until the login methods have loaded, at which point the card
  // commits to either setting a first password or changing an existing one.
  private readonly profileCommands = inject(ProfileCommandsService);
  private readonly loginMethods = loginMethodsResource();

  hasPassword = computed(() => this.loginMethods.value().hasPassword);

  private changePasswordLoading = this.profileCommands.isChangingPassword;

  private setPasswordLoading = this.profileCommands.isSettingPassword;

  private changePasswordError = this.profileCommands.changePasswordError;

  private setPasswordError = this.profileCommands.setPasswordError;

  loading = computed(() => {
    return this.hasPassword()
      ? this.changePasswordLoading()
      : this.setPasswordLoading();
  });

  error = computed(() => {
    const message = this.hasPassword()
      ? this.changePasswordError()
      : this.setPasswordError();

    return message ?? '';
  });

  newPasswordLabel = computed(() => {
    if (this.hasPassword()) {
      return $localize`:Label of the new password field:New Password`;
    }

    return $localize`:Label of the password field when setting one for the first time:Password`;
  });

  passwordFormModel = signal({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  });

  passwordForm = form(this.passwordFormModel, (schema) => {
    required(schema.currentPassword, { when: () => !!this.hasPassword() });
    required(schema.newPassword);
    required(schema.confirmPassword);
    maxLength(schema.currentPassword, 1024);
    maxLength(schema.newPassword, 1024);
    maxLength(schema.confirmPassword, 1024);
    validate(schema.confirmPassword, (context) => {
      if (context.value() === context.valueOf(schema.newPassword)) {
        return undefined;
      }

      return {
        kind: 'passwordMismatch',
        message: $localize`:Body of a dialog or validation message:Passwords do not match.`,
      };
    });
    disabled(schema, () => this.loading());
  });

  constructor() {
    // Setting a password flips the card into change mode, so clear the fields
    // rather than carrying them over into the form that replaces this one.
    effect(() => {
      this.hasPassword();

      this.passwordFormModel.set({
        currentPassword: '',
        newPassword: '',
        confirmPassword: '',
      });
    });
  }

  submitClicked(event: Event) {
    event.preventDefault();

    if (this.hasPassword()) {
      this.changePasswordClicked();
      return;
    }

    this.setPasswordClicked();
  }

  private changePasswordClicked() {
    const userId = inject(SessionService).currentUserId();

    if (!userId) return;

    submit(this.passwordForm, async () => {
      const request: ChangePasswordRequest = {
        userId,
        currentPassword: this.passwordForm.currentPassword().value(),
        newPassword: this.passwordForm.newPassword().value(),
      };

      this.profileCommands.changePassword(request);
    });
  }

  private setPasswordClicked() {
    submit(this.passwordForm, async () => {
      this.profileCommands.setPassword({
        password: this.passwordForm.newPassword().value(),
      });
    });
  }
}
