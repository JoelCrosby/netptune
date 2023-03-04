import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { requestPasswordReset } from '@core/auth/store/auth.actions';
import { selectRequestPasswordResetLoading } from '@core/auth/store/auth.selectors';
import { AppState } from '@core/core.state';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

@Component({
  selector: 'app-request-password-reset',
  templateUrl: './request-password-reset.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestPasswordResetComponent implements OnInit {
  authLoading$!: Observable<boolean>;

  formGroup = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
  });

  get email() {
    return this.formGroup.controls.email;
  }

  constructor(private store: Store<AppState>) {}

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
