import { inject } from '@angular/core';
import { CanActivateFn } from '@angular/router';
import { select, Store } from '@ngrx/store';
import { map } from 'rxjs/operators';
import { AppState } from '@core/core.state';
import { selectIsAuthenticated } from './store/auth.selectors';

export const loginGuard: CanActivateFn = () => {
  const store = inject(Store<AppState>);

  return store.pipe(
    select(selectIsAuthenticated),
    map((result) => !result)
  );
};
