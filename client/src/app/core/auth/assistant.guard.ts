import { inject } from '@angular/core';
import { SessionService } from '@core/services/session.service';
import { CanActivateFn, Router, RouterStateSnapshot } from '@angular/router';

export const assistantGuard: CanActivateFn = (
  _route,
  state: RouterStateSnapshot
) => {
  const router = inject(Router);
  const isAvailable = inject(SessionService).isAssistantAvailable();

  if (isAvailable) return true;

  const workspaceKey = state.url.split('?')[0].split('/').filter(Boolean)[0];

  return router.createUrlTree(['/', workspaceKey]);
};
