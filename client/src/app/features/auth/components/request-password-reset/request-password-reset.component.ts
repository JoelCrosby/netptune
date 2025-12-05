import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  effect,
  inject,
} from '@angular/core';
import {
  FormControl,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatAnchor, MatButton } from '@angular/material/button';
import { MatProgressBar } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { requestPasswordReset } from '@core/auth/store/auth.actions';
import { selectRequestPasswordResetLoading } from '@core/auth/store/auth.selectors';
import { Store } from '@ngrx/store';
import { FormErrorComponent } from '@static/components/form-error/form-error.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';

@Component({
  selector: 'app-request-password-reset',
  templateUrl: './request-password-reset.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    MatProgressBar,
    FormInputComponent,
    FormErrorComponent,
    MatAnchor,
    RouterLink,
    MatButton,
  ],
})
export class RequestPasswordResetComponent implements OnInit {
  private store = inject(Store);

  loading = this.store.selectSignal(selectRequestPasswordResetLoading);

  formGroup = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
  });

  get email() {
    return this.formGroup.controls.email;
  }

  ngOnInit() {
    effect(() => {
      if (this.loading()) {
        this.formGroup.disable();
      } else {
        this.formGroup.enable();
      }
    });
  }

  requestPasswordReset() {
    if (this.email.invalid) {
      this.email.markAsDirty();
      return;
    }

    const email = this.email.value as string;
    this.store.dispatch(requestPasswordReset({ email }));
  }
}
