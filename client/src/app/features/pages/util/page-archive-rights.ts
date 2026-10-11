import { inject } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { SessionService } from '@core/services/session.service';

// Mirrors the server rule: archive any page with pages.delete_any, only your own with pages.delete.
export function pageArchiveRights() {
  const canArchiveAny = hasPermission(PERMISSIONS.pages.deleteAny);
  const canArchiveOwn = hasPermission(PERMISSIONS.pages.delete);
  const currentUserId = inject(SessionService).currentUserId;

  return (createdByUserId: string | null): boolean => {
    const isOwnPage =
      createdByUserId !== null && createdByUserId === currentUserId();

    return canArchiveAny() || (canArchiveOwn() && isOwnPage);
  };
}
