import { Signal } from '@angular/core';
import { PERMISSIONS } from '@core/auth/permissions';
import { ClientResponse } from '@core/models/client-response';
import { permissionResource } from '@core/resources/permission.resource';
import { RoadmapViewModel } from '../models/roadmap.models';

export const roadmapResource = (query: Signal<string | undefined>) =>
  permissionResource<RoadmapViewModel | undefined>(
    PERMISSIONS.tasks.read,
    () => {
      const value = query();

      return value ? { url: `api/roadmap?${value}` } : undefined;
    },
    {
      parse: (response) =>
        (response as ClientResponse<RoadmapViewModel>).payload,
    }
  );
