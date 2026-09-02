import { HttpResourceRequest } from '@angular/common/http';
import { Signal } from '@angular/core';
import { PERMISSIONS } from '../auth/permissions';
import { ClientResponse } from '../models/client-response';
import { DEFAULT_PAGE_SIZE, Page } from '../models/pagination';
import { UserSelectOption } from '../models/view-models/user-select-option';
import { permissionResource } from './permission.resource';

export interface UserSelectQuery {
  search: string;
  enabled: boolean;
  excludeServiceAccounts: boolean;
}

export const userSelectResource = (query: Signal<UserSelectQuery>) => {
  return permissionResource<ClientResponse<Page<UserSelectOption>>>(
    PERMISSIONS.members.read,
    () => buildRequest(query())
  );
};

function buildRequest(query: UserSelectQuery): HttpResourceRequest | undefined {
  if (!query.enabled) return undefined;

  const search = query.search.trim();

  return {
    url: 'api/users/select',
    params: {
      page: 1,
      pageSize: DEFAULT_PAGE_SIZE,
      ...(search ? { search } : {}),
      ...(query.excludeServiceAccounts ? { excludeServiceAccounts: true } : {}),
    },
  };
}
