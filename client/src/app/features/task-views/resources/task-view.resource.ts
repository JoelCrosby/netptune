import { Signal } from '@angular/core';
import { PERMISSIONS } from '@core/auth/permissions';
import { ClientResponse } from '@core/models/client-response';
import { permissionResource } from '@core/resources/permission.resource';
import { TaskQueryCatalog, TaskView } from '../models/task-view.models';

export const taskQueryCatalogResource = () => {
  return permissionResource<TaskQueryCatalog>(
    PERMISSIONS.taskViews.read,
    () => ({ url: 'api/task-views/fields' }),
    {
      defaultValue: { fields: [], maximumDepth: 4, maximumConditionCount: 50 },
    }
  );
};

export const taskViewsResource = () => {
  return permissionResource<TaskView[]>(
    PERMISSIONS.taskViews.read,
    () => ({ url: 'api/task-views' }),
    { defaultValue: [], refreshOn: ['tasks'] }
  );
};

export const taskViewResource = (slug: Signal<string | undefined>) => {
  return permissionResource<ClientResponse<TaskView>>(
    PERMISSIONS.taskViews.read,
    () => {
      const key = slug();

      return key ? { url: `api/task-views/${key}` } : undefined;
    }
  );
};
