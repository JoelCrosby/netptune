import {
  Component,
  computed,
  effect,
  inject,
  OnDestroy,
  signal,
} from '@angular/core';
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
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';
import * as AuthActions from '@app/core/store/auth/auth.actions';
import { WorkspaceInvite } from '@app/core/store/auth/auth.models';
import { selectRegisterLoading } from '@app/core/store/auth/auth.selectors';
import { Store } from '@ngrx/store';
import { FormErrorsComponent } from '@static/components/form-error/form-errors.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { AuthPageContainerComponent } from '../auth-page-container/auth-page-container.component';
import { TurnstileComponent } from '../turnstile/turnstile.component';
import { AuthFormPanelComponent } from '../auth-form-panel/auth-form-panel.component';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';

@Component({
  selector: 'app-register',
  imports: [
    AuthPageContainerComponent,
    AuthFormPanelComponent,
    FormInputComponent,
    FormErrorsComponent,
    RouterLink,
    FlatButtonComponent,
    StrokedButtonComponent,
    FormField,
    FormErrorsComponent,
    TurnstileComponent,
  ],
  template: `<app-auth-page-container>
    <app-auth-form-panel
      showLogo
      heading="Create new Account"
      [loading]="loading()"
      (submitted)="register()">
      <app-form-input
        [formField]="registerForm.firstname"
        label="Firstname"
        maxLength="128"
        id="firstname"
        autocomplete="given-name">
      </app-form-input>

      <app-form-input
        [formField]="registerForm.lastname"
        label="Lastname"
        maxLength="128"
        id="lastname"
        autocomplete="family-name">
      </app-form-input>

      <app-form-input
        [formField]="registerForm.email"
        label="Email"
        maxLength="128"
        id="email"
        type="email"
        autocomplete="username">
      </app-form-input>

      <app-form-input
        [formField]="registerForm.password0"
        label="Password"
        maxLength="1024"
        id="new-password"
        autocomplete="new-password"
        type="password">
      </app-form-input>

      <app-form-input
        [formField]="registerForm.password1"
        label="Confirm Password"
        maxLength="1024"
        id="confirm-new-password"
        autocomplete="new-password"
        type="password">
        <app-form-errors [formField]="registerForm.password1" />
      </app-form-input>

      <app-turnstile (tokenGenerated)="onTurnstileResult($event)" />

      <div class="flex flex-row items-center justify-between gap-4">
        <a
          app-stroked-button
          color="primary"
          type="button"
          [routerLink]="['/auth/login']">
          Back to Log in
        </a>

        <button app-flat-button color="primary" type="submit">
          Create Account
        </button>
      </div>
    </app-auth-form-panel>
  </app-auth-page-container> `,
})
export class RegisterComponent implements OnDestroy {
  private store = inject(Store);
  private activatedRoute = toSignal(inject(ActivatedRoute).data);

  invite = computed(() => {
    const data = this.activatedRoute();
    const invite = data?.invite as WorkspaceInvite;

    if (invite?.success && invite?.email) {
      return invite;
    }

    return null;
  });

  loading = this.store.selectSignal(selectRegisterLoading);

  registerFormModel = signal({
    firstname: '',
    lastname: '',
    email: '',
    password0: '',
    password1: '',
    turnstile: '',
  });

  registerForm = form(this.registerFormModel, (schema) => {
    apply(
      schema.firstname,
      requiredTextSchema({ label: 'First name', maxLength: 128 })
    );
    apply(
      schema.lastname,
      requiredTextSchema({ label: 'Last name', maxLength: 128 })
    );
    required(schema.email, { message: 'Email is required.' });
    email(schema.email, { message: 'Enter a valid email address.' });
    maxLength(schema.email, 128);
    required(schema.password0, { message: 'Password is required.' });
    minLength(schema.password0, 4);
    maxLength(schema.password0, 1024);
    required(schema.password1, { message: 'Confirm your password.' });
    minLength(schema.password1, 4);
    maxLength(schema.password1, 1024);
    disabled(schema, () => this.loading());
    disabled(schema.email, () => !!this.invite()?.code);
    required(schema.turnstile);
    validate(schema.password1, (context) => {
      if (context.valueOf(schema.password0) !== context.value()) {
        return {
          kind: 'noMatch',
          message: 'Passwords do not match',
        };
      }

      return undefined;
    });
  });

  constructor() {
    effect(() => {
      const email = this.invite()?.email;

      if (email) {
        return this.registerForm.email().value.set(email);
      }
    });
  }

  ngOnDestroy() {
    this.store.dispatch(AuthActions.clearError({ error: 'registerError' }));
  }

  register() {
    submit(this.registerForm, async () => {
      const firstname = this.registerForm.firstname().value().trim();
      const lastname = this.registerForm.lastname().value().trim();
      const email = this.registerForm.email().value().trim();
      const password = this.registerForm.password0().value();
      const turnstile = this.registerForm.turnstile().value();
      const inviteCode = this.invite()?.code;

      this.store.dispatch(
        AuthActions.register.init({
          request: {
            firstname,
            lastname,
            email,
            password,
            inviteCode,
            turnstile,
          },
        })
      );
    });
  }

  onTurnstileResult(token: string) {
    this.registerFormModel.update((form) => ({ ...form, turnstile: token }));
  }
}
