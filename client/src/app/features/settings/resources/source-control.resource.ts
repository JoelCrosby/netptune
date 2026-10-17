import { PERMISSIONS } from '@core/auth/permissions';
import {
  SourceControlConnection,
  SourceControlRepo,
} from '@core/models/source-control';
import { permissionResource } from '@core/resources/permission.resource';

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
  return permissionResource<SourceControlRepo[]>({
    permission: PERMISSIONS.sourceControl.read,
    request: () => ({
      url: `api/source-control/connections/${connectionId()}/repos`,
    }),
    parse: (response) => response.payload ?? [],
    defaultValue: [],
    refreshOn: ['sourceControl'],
  });
};
