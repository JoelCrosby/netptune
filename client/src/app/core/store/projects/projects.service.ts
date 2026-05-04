import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { appendPageParams, MAX_PAGE_SIZE } from '@core/models/pagination';
import { AddProjectRequest } from '@core/models/project';
import { UpdateProjectRequest } from '@core/models/requests/upadte-project-request';
import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';

@Injectable({ providedIn: 'root' })
export class ProjectsService {
  private http = inject(HttpClient);

  get() {
    return this.http.get<ProjectViewModel[]>(`api/projects`, {
      params: appendPageParams(new HttpParams(), { pageSize: MAX_PAGE_SIZE }),
    });
  }

  getProjectDetail(projectKey: string) {
    return this.http.get<ProjectViewModel>(`api/projects/${projectKey}`);
  }

  getProjectBoards(projectId: number) {
    return this.http.get<BoardViewModel[]>(`api/boards/project/${projectId}`, {
      params: appendPageParams(new HttpParams(), { pageSize: MAX_PAGE_SIZE }),
    });
  }

  post(project: AddProjectRequest) {
    return this.http.post<ClientResponse<ProjectViewModel>>(
      'api/projects',
      project
    );
  }

  put(project: UpdateProjectRequest) {
    return this.http.put<ClientResponse<ProjectViewModel>>(
      'api/projects',
      project
    );
  }

  delete(project: ProjectViewModel) {
    return this.http.delete<ClientResponse>(`api/projects/${project.id}`);
  }
}
