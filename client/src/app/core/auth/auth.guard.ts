import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionService } from '@core/services/session.service';

export const authGuard: CanActivateFn = () => {
  const router = inject(Router);
  const isAuthenticated = inject(SessionService).isAuthenticated();

  return isAuthenticated || router.createUrlTree(['/auth/login']);
};
