import { WorkspaceAppUser } from '../models/appuser';
import { ClientResponse } from '../models/client-response';
import { MAX_PAGE_SIZE, Page } from '../models/pagination';
import { netptunePermissions } from '../auth/permissions';
import { permissionResource } from './permission-resource';

export const userResource = () => {
  return permissionResource<ClientResponse<Page<WorkspaceAppUser>>>(
    netptunePermissions.members.read,
    () => ({
      url: 'api/users',
      params: { page: 1, pageSize: MAX_PAGE_SIZE },
    })
  );
};
