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
    return this.http.post<Workspace>(
      environment.apiEndpoint + 'api/workspaces',
      workspace
    );
  }

  put(workspace: Workspace) {
    return this.http.put<Workspace>(
      environment.apiEndpoint + 'api/workspaces',
      workspace
    );
  }

  delete(workspace: Workspace) {
    return this.http.delete<Workspace>(
      environment.apiEndpoint + 'api/workspaces/' + workspace.id
    );
  }

  isSlugUnique(key: string) {
    return this.http.get<ClientResponse<IsSlugUniqueResponse>>(
      environment.apiEndpoint + `api/workspaces/is-unique/${key}`
    );
  }
}
