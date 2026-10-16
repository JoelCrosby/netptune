import { httpResource } from '@angular/common/http';
import { ClientResponse } from '../models/client-response';
import {
  SourceControlConnection,
  SourceControlProviderInfo,
  SourceControlRepo,
} from '../models/source-control';

export const sourceControlProvidersResource = () => {
  return httpResource<SourceControlProviderInfo[]>(
    () => ({ url: 'api/source-control/providers' }),
    { defaultValue: [] }
  );
};

export const sourceControlConnectionsResource = () => {
  return httpResource<SourceControlConnection[]>(
    () => ({ url: 'api/source-control/connections' }),
    { defaultValue: [] }
  );
};

export const sourceControlReposResource = (connectionId: () => number) => {
  return httpResource<ClientResponse<SourceControlRepo[]> | null>(
    () => ({ url: `api/source-control/connections/${connectionId()}/repos` }),
    { defaultValue: null }
  );
};
