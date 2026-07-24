import { SprintViewModel } from '../models/view-models/sprint-view-model';
import { SprintStatus } from '../enums/sprint-status';
import { netptunePermissions } from '../auth/permissions';
import { permissionResource } from './permission-resource';

export const sprintResource = (
  statuses: SprintStatus[] = [SprintStatus.planning, SprintStatus.active]
) => {
  return permissionResource<SprintViewModel[]>(
    netptunePermissions.sprints.read,
    () => ({
      url: 'api/sprints',
      params: {
        statuses,
        take: 100,
      },
    }),
    { defaultValue: [] }
  );
};
