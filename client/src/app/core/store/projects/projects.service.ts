import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { AddProjectRequest } from '@core/models/project';
import { UpdateProjectRequest } from '@core/models/requests/upadte-project-request';
import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { environment } from '@env/environment';

@Injectable({ providedIn: 'root' })
export class ProjectsService {
  private http = inject(HttpClient);


  get() {
    return this.http.get<ProjectViewModel[]>(
      environment.apiEndpoint + `api/projects`
    );
  }

  getProjectDetail(projectKey: string) {
    return this.http.get<ProjectViewModel>(
      environment.apiEndpoint + `api/projects/${projectKey}`
    );
  }

  getProjectBoards(projectId: number) {
    return this.http.get<BoardViewModel[]>(
      environment.apiEndpoint + `api/boards/project/${projectId}`
    );
  }

  post(project: AddProjectRequest) {
    return this.http.post<ClientResponse<ProjectViewModel>>(
      environment.apiEndpoint + 'api/projects',
      project
    );
  }

  put(project: UpdateProjectRequest) {
    return this.http.put<ClientResponse<ProjectViewModel>>(
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
