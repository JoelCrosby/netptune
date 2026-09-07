import { Status } from '../models/status';
import { MAX_PAGE_SIZE } from '../models/pagination';
import { EntityType } from '../models/entity-type';
import { PERMISSIONS } from '../auth/permissions';
import { permissionResource } from './permission.resource';

export const statusResource = () => {
  return permissionResource<Status[]>({
    permission: PERMISSIONS.statuses.read,
    request: () => ({
      url: 'api/statuses',
      params: { page: 1, pageSize: MAX_PAGE_SIZE, entityType: EntityType.task },
    }),
    defaultValue: [],
    refreshOn: ['statuses'],
  });
};
