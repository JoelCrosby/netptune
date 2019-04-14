import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Project } from '@app/core/models/project';
import { environment } from '@env/environment';
import { Workspace } from '@app/core/models/workspace';
import { throwError } from 'rxjs';

@Injectable()
export class ProjectsService {
  constructor(private http: HttpClient) {
    console.log('projects services constructor');
  }

  get(workspace: Workspace) {
    console.log('projects services get');
    if (!workspace) {
      throwError('current workspace undefined');
    } else {
      return this.http.get<Project[]>(
        environment.apiEndpoint + `api/projects?workspaceId=${workspace.id}`
      );
    }
  }
}
