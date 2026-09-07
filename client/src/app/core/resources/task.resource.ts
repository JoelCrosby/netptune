import { Signal } from '@angular/core';
import { PERMISSIONS } from '../auth/permissions';
import { TaskViewModel } from '../models/view-models/project-task-dto';
import { permissionResource, requestFrom } from './permission.resource';

export const taskDetailResource = (systemId: Signal<string | undefined>) => {
  return permissionResource<TaskViewModel | undefined>({
    permission: PERMISSIONS.tasks.read,
    request: requestFrom(systemId, (id) => ({
      url: 'api/tasks/detail',
      params: { systemId: id },
    })),
    refreshOn: ['tasks'],
  });
};
