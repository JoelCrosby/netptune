import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { select, Store } from '@ngrx/store';
import { tap } from 'rxjs/operators';
import { AppState } from '@core/core.state';
import { selectIsAuthenticated } from './store/auth.selectors';

export const authGuard: CanActivateFn = () => {
  const store = inject(Store<AppState>);
  const router = inject(Router);

  return store.pipe(
    select(selectIsAuthenticated),
    tap((state) => {
      if (!state) {
        void router.navigate(['/auth/login']);
      }
    })
  );
};
