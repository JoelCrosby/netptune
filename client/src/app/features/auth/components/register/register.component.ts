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
  customError,
  disabled,
  email,
  Field,
  form,
  maxLength,
  minLength,
  required,
  validate,
} from '@angular/forms/signals';
import { MatAnchor, MatButton } from '@angular/material/button';
import { MatProgressBar } from '@angular/material/progress-bar';
import { ActivatedRoute, RouterLink } from '@angular/router';
import * as AuthActions from '@core/auth/store/auth.actions';
import { WorkspaceInvite } from '@core/auth/store/auth.models';
import { selectRegisterLoading } from '@core/auth/store/auth.selectors';
import { Store } from '@ngrx/store';
import { FormErrorsComponent } from '@static/components/form-error/form-errors.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatProgressBar,
    FormInputComponent,
    FormErrorsComponent,
    MatAnchor,
    RouterLink,
    MatButton,
    Field,
    FormErrorsComponent,
  ],
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
      if (this.registerForm.password0().value() === value()) {
        return customError({
          kind: 'noMatch',
          message: 'Passwords do not match',
        });
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
