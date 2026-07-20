import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { first, map } from 'rxjs/operators';
import { selectIsAuthenticated } from '../store/auth/auth.selectors';

export const loginGuard: CanActivateFn = () => {
  const store = inject(Store);
  const router = inject(Router);

  return store.select(selectIsAuthenticated).pipe(
    first(),
    map((isAuthenticated) => {
      return isAuthenticated ? router.createUrlTree(['/']) : true;
    })
  );
};
