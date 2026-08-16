import { Signal } from '@angular/core';
import { PERMISSIONS } from '../auth/permissions';
import { TaskViewModel } from '../models/view-models/project-task-dto';
import { permissionResource } from './permission.resource';

export const taskDetailResource = (systemId: Signal<string | undefined>) => {
  return permissionResource<TaskViewModel | undefined>(
    PERMISSIONS.tasks.read,
    () => {
      const id = systemId();

      return id
        ? { url: 'api/tasks/detail', params: { systemId: id } }
        : undefined;
    },
    { refreshOn: ['tasks'] }
  );
};
