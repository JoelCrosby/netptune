import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';
import { clearError } from '@app/core/store/auth/auth.actions';
import { Store } from '@ngrx/store';

export const canDeactivateLogin: CanDeactivateFn<boolean> = () => {
  inject(Store).dispatch(clearError({ error: 'loginError' }));

  return true;
};
