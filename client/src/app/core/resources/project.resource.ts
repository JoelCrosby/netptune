import { httpResource } from '@angular/common/http';
import { MAX_PAGE_SIZE } from '../models/pagination';
import { ProjectViewModel } from '../models/view-models/project-view-model';

export const projectResource = () => {
  return httpResource<ProjectViewModel[]>(
    () => ({
      url: 'api/projects',
      params: { page: 1, pageSize: MAX_PAGE_SIZE },
    }),
    { defaultValue: [] }
  );
};
