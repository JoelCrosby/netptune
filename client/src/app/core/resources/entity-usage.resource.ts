import { Signal } from '@angular/core';
import { netptunePermissions } from '../auth/permissions';
import { EntityUsage } from '../models/entity-usage';
import { permissionResource } from './permission-resource';

export const statusUsageResource = (id: Signal<number | null>) => {
  return permissionResource<EntityUsage>(
    netptunePermissions.statuses.read,
    () => {
      const statusId = id();

      return statusId === null
        ? undefined
        : { url: `api/statuses/${statusId}/usage` };
    }
  );
};

export const tagUsageResource = (id: Signal<number | null>) => {
  return permissionResource<EntityUsage>(netptunePermissions.tags.read, () => {
    const tagId = id();

    return tagId === null ? undefined : { url: `api/tags/${tagId}/usage` };
  });
};

export const relationTypeUsageResource = (id: Signal<number | null>) => {
  return permissionResource<EntityUsage>(
    netptunePermissions.relationTypes.read,
    () => {
      const relationTypeId = id();

      return relationTypeId === null
        ? undefined
        : { url: `api/relation-types/${relationTypeId}/usage` };
    }
  );
};
