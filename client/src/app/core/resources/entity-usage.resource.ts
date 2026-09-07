import { Signal } from '@angular/core';
import { PERMISSIONS } from '../auth/permissions';
import { EntityUsage } from '../models/entity-usage';
import { permissionResource, requestFrom } from './permission.resource';

export const statusUsageResource = (id: Signal<number | null>) => {
  return permissionResource<EntityUsage>({
    permission: PERMISSIONS.statuses.read,
    request: requestFrom(id, (statusId) => ({
      url: `api/statuses/${statusId}/usage`,
    })),
    refreshOn: ['statuses', 'tasks'],
  });
};

export const tagUsageResource = (id: Signal<number | null>) => {
  return permissionResource<EntityUsage>({
    permission: PERMISSIONS.tags.read,
    request: requestFrom(id, (tagId) => ({ url: `api/tags/${tagId}/usage` })),
    refreshOn: ['tags', 'tasks'],
  });
};

export const relationTypeUsageResource = (id: Signal<number | null>) => {
  return permissionResource<EntityUsage>({
    permission: PERMISSIONS.relationTypes.read,
    request: requestFrom(id, (relationTypeId) => ({
      url: `api/relation-types/${relationTypeId}/usage`,
    })),
  });
};
