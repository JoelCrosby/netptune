import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
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

  delete(workspace: Workspace) {
    return this.http.delete<Workspace>(
      environment.apiEndpoint + 'api/workspaces/' + workspace.id
    );
  }
}
