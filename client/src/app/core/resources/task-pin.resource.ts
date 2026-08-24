import { Signal } from '@angular/core';
import { PERMISSIONS } from '@core/auth/permissions';
import { PinnedTask } from '@core/models/task-pin';
import { permissionResource } from './permission.resource';

export const pinnedTasksResource = () => {
  return permissionResource<PinnedTask[]>(
    PERMISSIONS.tasks.read,
    () => ({ url: 'api/pins' }),
    { defaultValue: [], refreshOn: ['tasks', 'pins'] }
  );
};

export const boardPinsResource = (boardId: Signal<number | undefined>) => {
  return permissionResource<PinnedTask[]>(
    PERMISSIONS.tasks.read,
    () => {
      const id = boardId();

      return id ? { url: `api/pins/board/${id}` } : undefined;
    },
    { defaultValue: [], refreshOn: ['tasks', 'pins'] }
  );
};
