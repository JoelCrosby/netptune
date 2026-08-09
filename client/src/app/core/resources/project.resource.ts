import { MAX_PAGE_SIZE } from '../models/pagination';
import { ProjectViewModel } from '../models/view-models/project-view-model';
import { PERMISSONS } from '../auth/permissions';
import { permissionResource } from './permission-resource';
import { Signal } from '@angular/core';

export const projectResource = () => {
  return permissionResource<ProjectViewModel[]>(
    PERMISSONS.projects.read,
    () => ({
      url: 'api/projects',
      params: { page: 1, pageSize: MAX_PAGE_SIZE },
    }),
    { defaultValue: [], refreshOn: ['projects'] }
  );
};

export const projectDetailResource = (keySignal: Signal<string>) => {
  return permissionResource<ProjectViewModel>(
    PERMISSONS.projects.read,
    () => ({
      url: `api/projects/${keySignal()}`,
    }),
    { refreshOn: ['projects'] }
  );
};
