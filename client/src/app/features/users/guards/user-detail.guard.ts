import { loadUser } from '@core/store/users/users.actions';
import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn } from '@angular/router';
import { Store } from '@ngrx/store';

export const userDetailGuard: CanActivateFn = (
  route: ActivatedRouteSnapshot
) => {
  const store = inject(Store);
  const userId = route.params?.['id'];

  if (!userId) return false;

  store.dispatch(loadUser({ userId }));

  return true;
};
