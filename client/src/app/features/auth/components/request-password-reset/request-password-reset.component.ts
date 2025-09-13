import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import {
  FormControl,
  FormGroup,
  Validators,
  FormsModule,
  ReactiveFormsModule,
} from '@angular/forms';
import { requestPasswordReset } from '@core/auth/store/auth.actions';
import { selectRequestPasswordResetLoading } from '@core/auth/store/auth.selectors';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
import { AsyncPipe } from '@angular/common';
import { MatProgressBar } from '@angular/material/progress-bar';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormErrorComponent } from '@static/components/form-error/form-error.component';
import { MatAnchor, MatButton } from '@angular/material/button';
import { RouterLink } from '@angular/router';

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
    AsyncPipe
],
})
export class RequestPasswordResetComponent implements OnInit {
  authLoading$!: Observable<boolean>;

  formGroup = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
  });

  get email() {
    return this.formGroup.controls.email;
  }

  constructor(private store: Store) {}

  ngOnInit() {
    this.authLoading$ = this.store
      .select(selectRequestPasswordResetLoading)
      .pipe(
        tap((loading) => {
          if (loading) return this.formGroup.disable();
          return this.formGroup.enable();
        })
      );
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
