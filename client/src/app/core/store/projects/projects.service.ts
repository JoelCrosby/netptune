import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { AddProjectRequest } from '@core/models/project';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { environment } from '@env/environment';

@Injectable({ providedIn: 'root' })
export class ProjectsService {
  constructor(private http: HttpClient) {}

  get() {
    return this.http.get<ProjectViewModel[]>(
      environment.apiEndpoint + `api/projects`
    );
  }

  post(project: AddProjectRequest) {
    return this.http.post<ProjectViewModel>(
      environment.apiEndpoint + 'api/projects',
      project
    );
  }

  delete(project: ProjectViewModel) {
    return this.http.delete<ClientResponse>(
      environment.apiEndpoint + `api/projects/${project.id}`
    );
  }
}
