import { inject } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivateFn } from '@angular/router';
import { loadProjectDetail } from '@core/store/projects/projects.actions';
import { Store } from '@ngrx/store';

export const projectDetailGuard: CanActivateFn = (
  route: ActivatedRouteSnapshot
) => {
  const store = inject(Store);
  const projectKey = route.params?.['id'];

  if (!projectKey) return false;

  store.dispatch(loadProjectDetail({ projectKey }));

  return true;
};
