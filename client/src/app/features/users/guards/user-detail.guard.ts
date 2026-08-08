import { ActivatedRouteSnapshot, CanActivateFn } from '@angular/router';

export const userDetailGuard: CanActivateFn = (
  route: ActivatedRouteSnapshot
) => {
  return !!route.params?.['id'];
};
