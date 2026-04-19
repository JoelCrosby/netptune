import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import { Store } from '@ngrx/store';
import { first, map } from 'rxjs/operators';
import { selectIsAuthenticated } from './store/auth.selectors';

export const loginGuard: CanActivateFn = () => {
  const store = inject(Store);

  return store.select(selectIsAuthenticated).pipe(
    first(),
    map((isAuthenticated) => !isAuthenticated)
  );
};
