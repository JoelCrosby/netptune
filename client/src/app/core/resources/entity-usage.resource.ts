import { Signal } from '@angular/core';
import { PERMISSIONS } from '../auth/permissions';
import { EntityUsage } from '../models/entity-usage';
import { permissionResource } from './permission.resource';

export const statusUsageResource = (id: Signal<number | null>) => {
  return permissionResource<EntityUsage>({
    permission: PERMISSIONS.statuses.read,
    request: () => {
      const statusId = id();

      return statusId === null
        ? undefined
        : { url: `api/statuses/${statusId}/usage` };
    },
    refreshOn: ['statuses', 'tasks'],
  });
};

export const tagUsageResource = (id: Signal<number | null>) => {
  return permissionResource<EntityUsage>({
    permission: PERMISSIONS.tags.read,
    request: () => {
      const tagId = id();

      return tagId === null ? undefined : { url: `api/tags/${tagId}/usage` };
    },
    refreshOn: ['tags', 'tasks'],
  });
};

export const relationTypeUsageResource = (id: Signal<number | null>) => {
  return permissionResource<EntityUsage>({
    permission: PERMISSIONS.relationTypes.read,
    request: () => {
      const relationTypeId = id();

      return relationTypeId === null
        ? undefined
        : { url: `api/relation-types/${relationTypeId}/usage` };
    },
  });
};
