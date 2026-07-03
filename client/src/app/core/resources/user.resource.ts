import { httpResource } from '@angular/common/http';
import { WorkspaceAppUser } from '../models/appuser';
import { ClientResponse } from '../models/client-response';
import { MAX_PAGE_SIZE, Page } from '../models/pagination';

export const userResource = () => {
  return httpResource<ClientResponse<Page<WorkspaceAppUser>>>(() => ({
    url: 'api/users',
    params: { page: 1, pageSize: MAX_PAGE_SIZE },
  }));
};
