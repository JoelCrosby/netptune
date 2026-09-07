import { Signal } from '@angular/core';
import { SprintDetailViewModel } from '../models/view-models/sprint-detail-view-model';
import { SprintViewModel } from '../models/view-models/sprint-view-model';
import { SprintStatus } from '../enums/sprint-status';
import { PERMISSIONS } from '../auth/permissions';
import { permissionResource } from './permission.resource';

export const sprintResource = (
  statuses: SprintStatus[] = [SprintStatus.planning, SprintStatus.active]
) => {
  return permissionResource<SprintViewModel[]>({
    permission: PERMISSIONS.sprints.read,
    request: () => ({
      url: 'api/sprints',
      params: {
        statuses,
        take: 100,
      },
    }),
    defaultValue: [],
    refreshOn: ['sprints'],
  });
};

export const currentSprintsResource = () => {
  return permissionResource<SprintViewModel[]>({
    permission: PERMISSIONS.sprints.read,
    request: () => ({
      url: 'api/sprints',
      params: { status: SprintStatus.active, take: 10 },
    }),
    defaultValue: [],
    refreshOn: ['sprints'],
  });
};

export const sprintDetailResource = (sprintId: Signal<number | undefined>) => {
  return permissionResource<SprintDetailViewModel | undefined>({
    permission: PERMISSIONS.sprints.read,
    request: () => {
      const id = sprintId();

      return id === undefined ? undefined : { url: `api/sprints/${id}` };
    },
    refreshOn: ['sprints', 'tasks'],
    parse: (response) => response.payload,
  });
};
