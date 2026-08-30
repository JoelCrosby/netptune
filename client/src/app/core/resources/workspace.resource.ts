import { httpResource } from '@angular/common/http';
import { inject } from '@angular/core';
import { MAX_PAGE_SIZE } from '@core/models/pagination';
import { Workspace } from '@core/models/workspace';
import { SessionService } from '@core/services/session.service';

export const workspacesResource = () => {
  const session = inject(SessionService);

  return httpResource<Workspace[]>(() => {
    if (!session.hasAuthSession()) {
      return undefined;
    }

    return {
      url: 'api/workspaces',
      params: { page: 1, pageSize: MAX_PAGE_SIZE },
    };
  });
};
