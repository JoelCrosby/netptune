import { MAX_PAGE_SIZE } from '../models/pagination';
import { ProjectViewModel } from '../models/view-models/project-view-model';
import { netptunePermissions } from '../auth/permissions';
import { permissionResource } from './permission-resource';

export const projectResource = () => {
  return permissionResource<ProjectViewModel[]>(
    netptunePermissions.projects.read,
    () => ({
      url: 'api/projects',
      params: { page: 1, pageSize: MAX_PAGE_SIZE },
    }),
    { defaultValue: [] }
  );
};
