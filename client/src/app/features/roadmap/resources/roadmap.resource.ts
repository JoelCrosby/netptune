import { Signal } from '@angular/core';
import { PERMISSIONS } from '@core/auth/permissions';
import {
  permissionResource,
  requestFrom,
} from '@core/resources/permission.resource';
import { RoadmapViewModel } from '../models/roadmap.models';

export const roadmapResource = (query: Signal<string | undefined>) =>
  permissionResource<RoadmapViewModel | undefined>({
    permission: PERMISSIONS.tasks.read,
    request: requestFrom(query, (value) => ({ url: `api/roadmap?${value}` })),
    parse: (response) => response.payload,
  });
