import { HttpClient } from '@angular/common/http';
import { Service, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import {
  ScmRepoSyncResult,
  SourceControlConnectCompletion,
  SourceControlConnection,
  SourceControlConnectStart,
  SourceControlIdentity,
  SourceControlProvider,
  SourceControlRepo,
} from '@core/models/source-control';

@Service()
export class SourceControlService {
  private readonly http = inject(HttpClient);

  beginConnect(provider: SourceControlProvider) {
    return this.http.post<ClientResponse<SourceControlConnectStart>>(
      'api/source-control/connections/begin',
      { provider }
    );
  }

  completeConnect(
    provider: SourceControlProvider,
    parameters: Record<string, string>,
    acceptWarnings = false
  ) {
    return this.http.post<ClientResponse<SourceControlConnectCompletion>>(
      'api/source-control/connections/complete',
      { provider, parameters, acceptWarnings }
    );
  }

  checkConnection(connectionId: number) {
    return this.http.post<ClientResponse<SourceControlConnection>>(
      `api/source-control/connections/${connectionId}/check`,
      null
    );
  }

  disconnect(connectionId: number) {
    return this.http.delete<ClientResponse<void>>(
      `api/source-control/connections/${connectionId}`
    );
  }

  sync(connectionId: number) {
    return this.http.post<ClientResponse<ScmRepoSyncResult>>(
      `api/source-control/connections/${connectionId}/sync`,
      null
    );
  }

  addIdentity(provider: SourceControlProvider, username: string) {
    return this.http.post<ClientResponse<SourceControlIdentity>>(
      'api/source-control/identities',
      { provider, username }
    );
  }

  removeIdentity(identityId: number) {
    return this.http.delete<ClientResponse<void>>(
      `api/source-control/identities/${identityId}`
    );
  }

  setLinked(repoId: number, isLinked: boolean) {
    return this.http.put<ClientResponse<SourceControlRepo>>(
      `api/source-control/repos/${repoId}/linked`,
      { isLinked }
    );
  }

  syncHistory(repoId: number) {
    return this.http.post<ClientResponse<SourceControlRepo>>(
      `api/source-control/repos/${repoId}/history-sync`,
      {}
    );
  }
}
