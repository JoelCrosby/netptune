import { Signal } from '@angular/core';
import { ClientResponse } from '../models/client-response';
import { MAX_PAGE_SIZE, Page } from '../models/pagination';
import { BoardViewModel } from '../models/view-models/board-view-model';
import { PERMISSIONS } from '../auth/permissions';
import { permissionResource } from './permission.resource';

export const workspaceBoardsResource = () => {
  return permissionResource<
    BoardViewModel[],
    ClientResponse<Page<BoardViewModel>>
  >({
    permission: PERMISSIONS.boards.read,
    request: () => ({
      url: 'api/boards',
      params: { page: 1, pageSize: MAX_PAGE_SIZE },
    }),
    defaultValue: [],
    refreshOn: ['boards'],
    parse: (response) => response.payload?.items ?? [],
  });
};

export const workspaceBoardsPageResource = (
  search: Signal<string>,
  page: Signal<number>
) => {
  return permissionResource<ClientResponse<Page<BoardViewModel>>>({
    permission: PERMISSIONS.boards.read,
    request: () => ({
      url: 'api/boards',
      params: { search: search(), page: page(), pageSize: MAX_PAGE_SIZE },
    }),
    refreshOn: ['boards'],
  });
};
