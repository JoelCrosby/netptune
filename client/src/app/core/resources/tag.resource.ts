import { httpResource } from '@angular/common/http';
import { MAX_PAGE_SIZE } from '../models/pagination';
import { Tag } from '../models/tag';

export const tagResource = () => {
  return httpResource<Tag[]>(
    () => ({
      url: 'api/tags/workspace',
      params: { page: 1, pageSize: MAX_PAGE_SIZE },
    }),
    { defaultValue: [] }
  );
};
