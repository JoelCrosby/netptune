import { Signal } from '@angular/core';
import { netptunePermissions } from '../auth/permissions';
import { TaskViewModel } from '../models/view-models/project-task-dto';
import { permissionResource } from './permission-resource';

export const taskDetailResource = (systemId: Signal<string | undefined>) => {
  return permissionResource<TaskViewModel | undefined>(
    netptunePermissions.tasks.read,
    () => {
      const id = systemId();

      return id
        ? { url: 'api/tasks/detail', params: { systemId: id } }
        : undefined;
    },
    { refreshOn: ['tasks'] }
  );
};
