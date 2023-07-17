import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import { select, Store } from '@ngrx/store';
import { map } from 'rxjs/operators';
import { selectIsAuthenticated } from './store/auth.selectors';

export const loginGuard: CanActivateFn = () => {
  const store = inject(Store);

  return store.pipe(
    select(selectIsAuthenticated),
    map((result) => !result)
  );
};
