import { httpResource } from '@angular/common/http';
import { Signal, debounced } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import {
  DEFAULT_VIEW_PAGE_SIZE,
  TaskQueryGroup,
  TaskViewResult,
} from '../models/task-view.models';

export interface TaskQueryPreviewRequest {
  query: TaskQueryGroup;
  page?: number;
  pageSize?: number;
  sortBy?: string | null;
  sortDirection?: string | null;
}

const emptyResult: TaskViewResult = {
  items: [],
  page: 1,
  pageSize: DEFAULT_VIEW_PAGE_SIZE,
  totalCount: 0,
  totalPages: 0,
  errors: [],
};

export const taskQueryPreviewResource = (
  request: Signal<TaskQueryPreviewRequest | undefined>
) => {
  const settled = debounced(request, 350);

  return httpResource<ClientResponse<TaskViewResult>>(
    () => {
      const body = settled.value();

      if (!body) return undefined;

      return { url: 'api/task-views/preview', method: 'POST', body };
    },
    {
      defaultValue: { isSuccess: true, payload: emptyResult },
      parse: (response) => response as ClientResponse<TaskViewResult>,
    }
  );
};
