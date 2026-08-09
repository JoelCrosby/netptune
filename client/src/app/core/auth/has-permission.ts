import {
  assertInInjectionContext,
  computed,
  inject,
  Signal,
} from '@angular/core';
import { Permission } from '@core/auth/permissions';
import { SessionService } from '@core/services/session.service';

export function hasPermission(permission: Permission): Signal<boolean> {
  assertInInjectionContext(hasPermission);

  const session = inject(SessionService);

  return computed(() => session.can(permission));
}
