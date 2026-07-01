import { httpResource } from '@angular/common/http';
import { Status } from '../models/status';
import { MAX_PAGE_SIZE } from '../models/pagination';
import { EntityType } from '../models/entity-type';

export const statusResource = () => {
  return httpResource<Status[]>(() => ({
    url: 'api/statuses',
    params: { page: 1, pageSize: MAX_PAGE_SIZE, entityType: EntityType.task },
  }));
};
