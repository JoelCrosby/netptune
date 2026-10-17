import { Signal } from '@angular/core';
import { PERMISSIONS } from '../auth/permissions';
import { TaskCodeLink } from '../models/source-control';
import { permissionResource, requestFrom } from './permission.resource';

export const taskCodeLinksResource = (systemId: Signal<string | undefined>) => {
  return permissionResource<TaskCodeLink[]>({
    permission: PERMISSIONS.sourceControl.read,
    request: requestFrom(systemId, (id) => ({
      url: `api/source-control/tasks/${id}/links`,
    })),
    parse: (response) => response.payload ?? [],
    defaultValue: [],
    refreshOn: ['codeLinks'],
  });
};
