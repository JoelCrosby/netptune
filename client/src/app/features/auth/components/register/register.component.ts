import { Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  apply,
  disabled,
  email,
  form,
  FormField,
  maxLength,
  minLength,
  required,
  submit,
  validate,
} from '@angular/forms/signals';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { WorkspaceInvite } from '@core/models/session';
import { AuthCommandsService } from '@core/services/auth-commands.service';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { TextLinkComponent } from '@static/components/text-link.component';
import { AuthFieldComponent } from '../auth-field/auth-field.component';
import { AuthFormPanelComponent } from '../auth-form-panel/auth-form-panel.component';
import { AuthPageContainerComponent } from '../auth-page-container/auth-page-container.component';
import { LoginProvidersComponent } from '../login/login-providers.component';
import { PasswordStrengthMeterComponent } from '../password-strength-meter/password-strength-meter.component';
import { TurnstileComponent } from '../turnstile/turnstile.component';

const PASSWORD_MIN_LENGTH = 8;

@Component({
  selector: 'app-register',
  imports: [
    AuthPageContainerComponent,
    AuthFormPanelComponent,
    AuthFieldComponent,
    CheckboxComponent,
    FlatButtonComponent,
    LoginProvidersComponent,
    PasswordStrengthMeterComponent,
    TextLinkComponent,
    RouterLink,
    FormField,
    TurnstileComponent,
  ],
  template: `
    <app-auth-page-container>
      <app-auth-form-panel
        showLogo
        i18n-eyebrow="Label above the heading of the sign-in form"
        eyebrow="Netptune account"
        i18n-heading="Heading of the account registration form"
        heading="Create your account"
        [loading]="loading()"
        (submitted)="register()">
        <p panelSubtitle>
          <span i18n="Sits before the link to the login form">
            Already have one?
          </span>
          <a app-text-link [routerLink]="['/auth/login']">
            <span i18n="Link from the registration form back to the login form">
              Sign in
            </span>
          </a>
        </p>

        <div class="mt-5.5 flex flex-col gap-4">
          <div class="flex flex-col gap-4 sm:flex-row">
            <app-auth-field
              [formField]="registerForm.firstname"
              i18n-label="
                Label of the given-name field on the registration form
              "
              label="First name"
              maxLength="128"
              id="firstname"
              autocomplete="given-name" />

            <app-auth-field
              [formField]="registerForm.lastname"
              i18n-label="
                Label of the family-name field on the registration form
              "
              label="Last name"
              maxLength="128"
              id="lastname"
              autocomplete="family-name" />
          </div>

          <app-auth-field
            [formField]="registerForm.email"
            i18n-label="Label of the work e-mail field on the registration form"
            label="Work email"
            i18n-placeholder="
              Placeholder of the e-mail address field on auth forms
            "
            placeholder="you@company.com"
            maxLength="128"
            id="email"
            type="email"
            autocomplete="username" />

          <app-auth-field
            revealable
            [formField]="registerForm.password"
            i18n-label="Label of the password field on the registration form"
            label="Password"
            i18n-placeholder="
              Placeholder of the password field on the registration form
            "
            placeholder="At least 8 characters"
            maxLength="1024"
            id="new-password"
            type="password"
            autocomplete="new-password">
            <app-password-strength-meter
              [password]="registerForm.password().value()" />
          </app-auth-field>

          <app-checkbox
            density="compact"
            [checked]="registerForm.agreedToTerms().value()"
            (changed)="registerForm.agreedToTerms().value.set($event)">
            <ng-container
              i18n="
                Checkbox confirming the terms of service on the registration
                form
              ">
              I agree to the terms of service and privacy policy
            </ng-container>
          </app-checkbox>

          <app-turnstile (tokenGenerated)="onTurnstileResult($event)" />

          <button
            app-flat-button
            color="primary"
            size="large"
            block
            type="submit"
            [disabled]="!canSubmit()">
            {{ submitLabel() }}
          </button>
        </div>

        <app-login-providers />

        <p panelFootnote>
          <ng-container
            i18n="
              Note in the footer of the registration
              card@@auth.register.inviteNote">
            Workspace invitations are sent to the address you verify.
          </ng-container>
        </p>
      </app-auth-form-panel>
    </app-auth-page-container>
  `,
})
export class RegisterComponent {
  private auth = inject(AuthCommandsService);
  private activatedRoute = toSignal(inject(ActivatedRoute).data);

  invite = computed(() => {
    const data = this.activatedRoute();
    const invite = data?.invite as WorkspaceInvite;

    if (invite?.success && invite?.email) {
      return invite;
    }

    return null;
  });

  loading = this.auth.registerLoading;

  submitLabel = computed(() => {
    if (this.loading()) {
      return $localize`:Submit button on the registration form while the account is created:Creating account…`;
    }

    return $localize`:Submit button on the account registration form:Create account`;
  });

  registerFormModel = signal({
    firstname: '',
    lastname: '',
    email: '',
    password: '',
    agreedToTerms: false,
    turnstile: '',
  });

  registerForm = form(this.registerFormModel, (schema) => {
    apply(
      schema.firstname,
      requiredTextSchema({
        label: $localize`:Field name used inside registration validation messages, e.g. "First name is required.":First name`,
        maxLength: 128,
      })
    );
    apply(
      schema.lastname,
      requiredTextSchema({
        label: $localize`:Field name used inside registration validation messages, e.g. "Last name is required.":Last name`,
        maxLength: 128,
      })
    );
    required(schema.email, {
      message: $localize`:Validation error when the e-mail field is empty:Email is required.`,
    });
    email(schema.email, {
      message: $localize`:Validation error when the e-mail field is not a valid address:Enter a valid email address.`,
    });
    maxLength(schema.email, 128);
    required(schema.password, {
      message: $localize`:Validation error when the password field is empty:Password is required.`,
    });
    minLength(schema.password, PASSWORD_MIN_LENGTH, {
      message: $localize`:Validation error when a new password is too short:Use at least 8 characters.`,
    });
    maxLength(schema.password, 1024);
    validate(schema.agreedToTerms, (context) => {
      if (context.value()) return undefined;

      return {
        kind: 'termsNotAccepted',
        message: $localize`:Validation error when the terms of service are not accepted:Accept the terms of service to continue.`,
      };
    });
    disabled(schema, () => this.loading());
    disabled(schema.email, () => !!this.invite()?.code);
    required(schema.turnstile);
  });

  // The bot check is deliberately left out — it resolves on its own, and gating the
  // button on it would leave nothing to click when it fails to load.
  canSubmit = computed(() => {
    return (
      this.registerForm.firstname().valid() &&
      this.registerForm.lastname().valid() &&
      this.registerForm.email().valid() &&
      this.registerForm.password().valid() &&
      this.registerForm.agreedToTerms().valid() &&
      !this.loading()
    );
  });

  constructor() {
    effect(() => {
      const email = this.invite()?.email;

      if (email) {
        return this.registerForm.email().value.set(email);
      }
    });
  }

  register() {
    submit(this.registerForm, async () => {
      const firstname = this.registerForm.firstname().value().trim();
      const lastname = this.registerForm.lastname().value().trim();
      const email = this.registerForm.email().value().trim();
      const password = this.registerForm.password().value();
      const turnstile = this.registerForm.turnstile().value();
      const inviteCode = this.invite()?.code;

      this.auth.register({
        firstname,
        lastname,
        email,
        password,
        inviteCode,
        turnstile,
      });
    });
  }

  onTurnstileResult(token: string) {
    this.registerFormModel.update((form) => ({ ...form, turnstile: token }));
  }
}
