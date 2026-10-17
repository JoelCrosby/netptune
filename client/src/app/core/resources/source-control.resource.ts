import { httpResource } from '@angular/common/http';
import { PERMISSIONS } from '../auth/permissions';
import { ClientResponse } from '../models/client-response';
import {
  SourceControlConnection,
  SourceControlProviderInfo,
  SourceControlRepo,
} from '../models/source-control';
import { permissionResource } from './permission.resource';

export const sourceControlProvidersResource = () => {
  return httpResource<SourceControlProviderInfo[]>(
    () => ({ url: 'api/source-control/providers' }),
    { defaultValue: [] }
  );
};

export const sourceControlConnectionsResource = () => {
  return permissionResource<
    SourceControlConnection[],
    SourceControlConnection[]
  >({
    permission: PERMISSIONS.sourceControl.read,
    request: () => ({ url: 'api/source-control/connections' }),
    defaultValue: [],
    refreshOn: ['sourceControl'],
  });
};

export const sourceControlReposResource = (connectionId: () => number) => {
  return httpResource<ClientResponse<SourceControlRepo[]> | null>(
    () => ({ url: `api/source-control/connections/${connectionId()}/repos` }),
    { defaultValue: null }
  );
};
