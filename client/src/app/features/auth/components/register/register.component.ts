import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  OnDestroy,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  disabled,
  email,
  form,
  FormField,
  maxLength,
  minLength,
  required,
  validate,
} from '@angular/forms/signals';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';
import { ProgressBarComponent } from '@app/static/components/progress-bar/progress-bar.component';
import * as AuthActions from '@core/auth/store/auth.actions';
import { WorkspaceInvite } from '@core/auth/store/auth.models';
import { selectRegisterLoading } from '@core/auth/store/auth.selectors';
import { Store } from '@ngrx/store';
import { FormErrorsComponent } from '@static/components/form-error/form-errors.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { AuthPageContainerComponent } from '../auth-page-container/auth-page-container.component';

@Component({
  selector: 'app-register',
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
    FormErrorsComponent,
  ],
  template: `<app-auth-page-container>
    <form
      (submit)="register($event)"
      class="bg-background border-border z-1 flex w-md flex-col gap-4 rounded border p-8 shadow-lg">
      <div>
        @if (loading()) {
          <app-progress-bar mode="indeterminate" />
        }
      </div>

      <img
        src="assets/apple-touch-icon.png"
        alt="Netptune Logo"
        width="72"
        height="72" />

      <h3 class="mb-[2.8rem] w-full text-center font-normal tracking-normal">
        Create new Account
      </h3>

      <app-form-input
        [formField]="registerForm.firstname"
        label="Firstname"
        maxLength="1024"
        id="firstname"
        autocomplete="given-name">
      </app-form-input>

      <app-form-input
        [formField]="registerForm.lastname"
        label="Lastname"
        maxLength="1024"
        id="lastname"
        autocomplete="family-name">
      </app-form-input>

      <app-form-input
        [formField]="registerForm.email"
        label="Email"
        maxLength="1024"
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
    </form>
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
  });

  registerForm = form(this.registerFormModel, (schema) => {
    required(schema.firstname);
    maxLength(schema.firstname, 128);
    required(schema.lastname);
    maxLength(schema.lastname, 128);
    required(schema.email);
    email(schema.email);
    maxLength(schema.email, 128);
    required(schema.password0);
    minLength(schema.password0, 4);
    required(schema.password1);
    minLength(schema.password1, 4);
    disabled(schema, () => this.loading());
    disabled(schema.email, () => !!this.invite()?.code);
    validate(schema.password1, ({ value }) => {
      if (this.registerForm.password0().value() !== value()) {
        return {
          kind: 'noMatch',
          message: 'Passwords do not match',
        };
      }

      return null;
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

  register(event: Event) {
    event.preventDefault();

    const firstname = this.registerForm.firstname().value();
    const lastname = this.registerForm.lastname().value();
    const email = this.registerForm.email().value();
    const password = this.registerForm.password0().value();

    const inviteCode = this.invite()?.code;

    if (this.registerForm().invalid()) {
      this.registerForm().markAsDirty();
      return;
    }

    this.store.dispatch(
      AuthActions.register({
        request: {
          firstname,
          lastname,
          email,
          password,
          inviteCode,
        },
      })
    );
  }
}
