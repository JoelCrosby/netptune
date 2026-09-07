import { Signal } from '@angular/core';
import { PERMISSIONS } from '@core/auth/permissions';
import { PinnedTask } from '@core/models/task-pin';
import { permissionResource, requestFrom } from './permission.resource';

export const pinnedTasksResource = () => {
  return permissionResource<PinnedTask[]>({
    permission: PERMISSIONS.tasks.read,
    request: () => ({ url: 'api/pins' }),
    defaultValue: [],
    refreshOn: ['tasks', 'pins'],
  });
};

export const boardPinsResource = (boardId: Signal<number | undefined>) => {
  return permissionResource<PinnedTask[]>({
    permission: PERMISSIONS.tasks.read,
    request: requestFrom(boardId, (id) => ({ url: `api/pins/board/${id}` })),
    defaultValue: [],
    refreshOn: ['tasks', 'pins'],
    parse: (response) => response.payload ?? [],
  });
};
