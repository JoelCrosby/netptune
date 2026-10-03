import { requestFrom } from '@core/resources/permission.resource';
import { httpResource } from '@angular/common/http';
import { Signal, debounced } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import {
  DEFAULT_QUERY_PAGE_SIZE,
  TaskQueryGroup,
  SavedQueryResult,
} from '../models/saved-query.models';

export interface TaskQueryPreviewRequest {
  query: TaskQueryGroup;
  page?: number;
  pageSize?: number;
  sortBy?: string | null;
  sortDirection?: string | null;
}

const emptyResult: SavedQueryResult = {
  items: [],
  page: 1,
  pageSize: DEFAULT_QUERY_PAGE_SIZE,
  totalCount: 0,
  totalPages: 0,
  errors: [],
};

export const taskQueryPreviewResource = (
  request: Signal<TaskQueryPreviewRequest | undefined>
) => {
  const settled = debounced(request, 350);

  return httpResource<ClientResponse<SavedQueryResult>>(
    requestFrom(settled.value, (body) => ({
      url: 'api/task-views/preview',
      method: 'POST',
      body,
    })),
    {
      defaultValue: { isSuccess: true, payload: emptyResult },
      parse: (response) => response as ClientResponse<SavedQueryResult>,
    }
  );
};
