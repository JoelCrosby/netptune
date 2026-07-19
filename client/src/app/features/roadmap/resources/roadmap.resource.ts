import { Signal } from '@angular/core';
import { netptunePermissions } from '@core/auth/permissions';
import { permissionResource } from '@core/resources/permission-resource';
import { RoadmapViewModel } from '../models/roadmap.models';

export const roadmapResource = (query: Signal<string>) =>
  permissionResource<RoadmapViewModel>(netptunePermissions.tasks.read, () => ({
    url: `api/roadmap?${query()}`,
  }));
