import { PERMISSIONS } from '../auth/permissions';
import { MAX_PAGE_SIZE } from '../models/pagination';
import { Tag } from '../models/tag';
import { permissionResource } from './permission.resource';

export const tagResource = () => {
  return permissionResource<Tag[]>(
    PERMISSIONS.tags.read,
    () => ({
      url: 'api/tags/workspace',
      params: { page: 1, pageSize: MAX_PAGE_SIZE },
    }),
    { defaultValue: [], refreshOn: ['tags'] }
  );
};
