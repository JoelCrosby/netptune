import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { IsSlugUniqueResponse } from '@core/models/is-slug-unique-response';
import { AddWorkspaceRequest } from '@core/models/requests/add-workspace-request';
import { UpdateWorkspaceRequest } from '@core/models/requests/update-workspace-request';
import { Workspace } from '@core/models/workspace';
import { throwError } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class WorkspacesService {
  private http = inject(HttpClient);

  get() {
    return this.http.get<Workspace[]>('api/workspaces');
  }

  post(request: AddWorkspaceRequest) {
    return this.http.post<ClientResponse<Workspace>>('api/workspaces', request);
  }

  put(request: UpdateWorkspaceRequest) {
    return this.http.put<ClientResponse<Workspace>>('api/workspaces', request);
  }

  delete(workspace: Workspace) {
    if (!workspace?.slug) {
      return throwError(() => new Error('workspace slug empty'));
    }

    return this.http.delete<ClientResponse>('api/workspaces/' + workspace.slug);
  }

  isSlugUnique(key: string) {
    return this.http.get<ClientResponse<IsSlugUniqueResponse>>(
      `api/workspaces/is-unique/${key}`
    );
  }
}
