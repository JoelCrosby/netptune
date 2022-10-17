import { throwError } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { IsSlugUniqueResponse } from '@core/models/is-slug-unique-response';
import { Workspace } from '@core/models/workspace';
import { environment } from '@env/environment';

@Injectable({ providedIn: 'root' })
export class WorkspacesService {
  constructor(private http: HttpClient) {}

  get() {
    return this.http.get<Workspace[]>(
      environment.apiEndpoint + 'api/workspaces'
    );
  }

  post(workspace: Workspace) {
    return this.http.post<ClientResponse<Workspace>>(
      environment.apiEndpoint + 'api/workspaces',
      workspace
    );
  }

  put(workspace: Workspace) {
    return this.http.put<ClientResponse<Workspace>>(
      environment.apiEndpoint + 'api/workspaces',
      workspace
    );
  }

  delete(workspace: Workspace) {
    if (!workspace?.slug) return throwError('workspace slug empty');

    return this.http.delete<ClientResponse>(
      environment.apiEndpoint + 'api/workspaces/' + workspace.slug
    );
  }

  isSlugUnique(key: string) {
    return this.http.get<ClientResponse<IsSlugUniqueResponse>>(
      environment.apiEndpoint + `api/workspaces/is-unique/${key}`
    );
  }
}
