import { httpResource } from '@angular/common/http';
import { Signal } from '@angular/core';
import { PERMISSIONS } from '../auth/permissions';
import {
  SourceControlConnection,
  SourceControlIdentity,
  SourceControlProviderInfo,
  SourceControlRepo,
  TaskCodeLink,
} from '../models/source-control';
import { permissionResource, requestFrom } from './permission.resource';

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

export const sourceControlIdentitiesResource = () => {
  return permissionResource<SourceControlIdentity[], SourceControlIdentity[]>({
    request: () => ({ url: 'api/source-control/identities' }),
    defaultValue: [],
    refreshOn: ['codeLinks'],
  });
};

export const taskCodeLinksResource = (systemId: Signal<string | undefined>) => {
  return permissionResource<TaskCodeLink[]>({
    permission: PERMISSIONS.sourceControl.read,
    request: requestFrom(systemId, (id) => ({
      url: `api/source-control/tasks/${id}/links`,
    })),
    parse: (response) => response.payload ?? [],
    defaultValue: [],
    refreshOn: ['codeLinks'],
  });
};
