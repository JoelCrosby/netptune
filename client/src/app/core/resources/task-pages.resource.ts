import { Signal } from '@angular/core';
import { PERMISSIONS } from '@core/auth/permissions';
import { PageBacklink } from '@core/models/pages';
import {
  permissionResource,
  requestFrom,
} from '@core/resources/permission.resource';

export const taskPagesResource = (systemId: Signal<string | undefined>) => {
  return permissionResource<PageBacklink[]>({
    permission: PERMISSIONS.pages.read,
    request: requestFrom(systemId, (id) => ({
      url: `api/pages/linked-to-task/${id}`,
    })),
    parse: (response) => response.payload ?? [],
    defaultValue: [],
    refreshOn: ['pages'],
  });
};
