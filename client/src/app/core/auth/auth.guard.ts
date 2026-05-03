import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { of } from 'rxjs';
import { catchError, first, map, switchMap } from 'rxjs/operators';
import { AuthService } from './auth.service';
import { currentUserSuccess } from '../store/auth/auth.actions';
import {
  selectCurrentUserPermissions,
  selectIsAuthenticated,
} from '../store/auth/auth.selectors';

export const authGuard: CanActivateFn = () => {
  const store = inject(Store);
  const router = inject(Router);
  const authService = inject(AuthService);

  return store.select(selectIsAuthenticated).pipe(
    first(),
    switchMap((isAuthenticated) => {
      if (!isAuthenticated) {
        return of(router.createUrlTree(['/auth/login']));
      }

      return store.select(selectCurrentUserPermissions).pipe(
        first(),
        switchMap((permissions) => {
          if (permissions) return of(true);

          return authService.currentUser().pipe(
            map((user) => {
              store.dispatch(currentUserSuccess({ user }));

              return true;
            }),
            catchError(() => of(router.createUrlTree(['/auth/login'])))
          );
        })
      );
    })
  );
};
