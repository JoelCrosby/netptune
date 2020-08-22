import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { requestPasswordReset } from '@app/core/auth/store/auth.actions';
import { selectRequestPasswordResetLoading } from '@core/auth/store/auth.selectors';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

@Component({
  selector: 'app-request-password-reset',
  templateUrl: './request-password-reset.component.html',
  styleUrls: ['./request-password-reset.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestPasswordResetComponent implements OnInit {
  authLoading$: Observable<boolean>;

  requestPasswordResetGroup = new FormGroup({
    email: new FormControl('', [Validators.required, Validators.email]),
  });

  get email() {
    return this.requestPasswordResetGroup.get('email');
  }

  constructor(private store: Store) {}

  ngOnInit() {
    this.authLoading$ = this.store
      .select(selectRequestPasswordResetLoading)
      .pipe(
        tap((loading) => {
          if (loading) return this.requestPasswordResetGroup.disable();
          return this.requestPasswordResetGroup.enable();
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
