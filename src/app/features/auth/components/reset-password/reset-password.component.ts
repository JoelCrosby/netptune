import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Store } from '@ngrx/store';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { ActivatedRoute } from '@angular/router';
import { first, tap } from 'rxjs/operators';
import { ResetPasswordRequest } from '@app/core/auth/store/auth.models';
import { resetPassword } from '@app/core/auth/store/auth.actions';

@Component({
  selector: 'app-reset-password',
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResetPasswordComponent implements OnInit {
  $authLoading: Observable<boolean>;

  request: ResetPasswordRequest;

  passwordResetGroup = new FormGroup({
    password0: new FormControl('', [
      Validators.required,
      Validators.minLength(4),
    ]),
    password1: new FormControl('', [
      Validators.required,
      Validators.minLength(4),
    ]),
  });

  get password0() {
    return this.passwordResetGroup.get('password0');
  }

  get password1() {
    return this.passwordResetGroup.get('password1');
  }

  constructor(private activatedRoute: ActivatedRoute, private store: Store) {
    this.activatedRoute.data
      .pipe(
        first(),
        tap((data) => {
          this.request = data.resetPassword as ResetPasswordRequest;
        })
      )
      .subscribe();
  }

  ngOnInit() {}

  resetPassword() {
    if (this.password0.invalid || this.password1.invalid) {
      this.password0.markAsDirty();
      this.password1.markAsDirty();
      return;
    }

    if (this.password0.value !== this.password1.value) {
      this.password0.markAsDirty();
      this.password1.setErrors({ noMatch: true });
      this.password1.markAsDirty();
      return;
    }

    const request: ResetPasswordRequest = {
      ...this.request,
      password: this.password0.value,
    };

    this.store.dispatch(resetPassword({ request }));
  }
}
