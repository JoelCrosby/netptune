import { httpResource } from '@angular/common/http';
import { MAX_PAGE_SIZE } from '@core/models/pagination';
import { Workspace } from '@core/models/workspace';

export const workspacesResource = () => {
  return httpResource<Workspace[]>(() => ({
    url: 'api/workspaces',
    params: { page: 1, pageSize: MAX_PAGE_SIZE },
  }));
};
