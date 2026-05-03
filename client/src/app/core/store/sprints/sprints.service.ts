import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { AddSprintRequest } from '@core/models/requests/add-sprint-request';
import { AddTasksToSprintRequest } from '@core/models/requests/add-tasks-to-sprint-request';
import { UpdateSprintRequest } from '@core/models/requests/update-sprint-request';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { SprintFilter } from './sprints.model';

@Injectable({ providedIn: 'root' })
export class SprintsService {
  private http = inject(HttpClient);

  get(filter?: SprintFilter) {
    let params = new HttpParams();

    if (filter?.projectId !== undefined) {
      params = params.set('projectId', filter.projectId);
    }

    if (filter?.status !== undefined) {
      params = params.set('status', filter.status);
    }

    if (filter?.take !== undefined) {
      params = params.set('take', filter.take);
    }

    return this.http.get<SprintViewModel[]>('api/sprints', { params });
  }

  detail(sprintId: number) {
    return this.http.get<ClientResponse<SprintDetailViewModel>>(
      `api/sprints/${sprintId}`
    );
  }

  availableTasks(sprintId: number, projectId: number) {
    const params = new HttpParams()
      .set('projectId', projectId)
      .set('excludeSprintId', sprintId)
      .set('take', 100);

    return this.http.get<TaskViewModel[]>('api/tasks', { params });
  }

  post(request: AddSprintRequest) {
    return this.http.post<ClientResponse<SprintViewModel>>(
      'api/sprints',
      request
    );
  }

  put(request: UpdateSprintRequest) {
    return this.http.put<ClientResponse<SprintViewModel>>(
      'api/sprints',
      request
    );
  }

  delete(sprintId: number) {
    return this.http.delete<ClientResponse>(`api/sprints/${sprintId}`);
  }

  start(sprintId: number) {
    return this.http.post<ClientResponse<SprintViewModel>>(
      `api/sprints/${sprintId}/start`,
      null
    );
  }

  complete(sprintId: number) {
    return this.http.post<ClientResponse<SprintViewModel>>(
      `api/sprints/${sprintId}/complete`,
      null
    );
  }

  addTasks(sprintId: number, request: AddTasksToSprintRequest) {
    return this.http.post<ClientResponse<SprintDetailViewModel>>(
      `api/sprints/${sprintId}/tasks`,
      request
    );
  }

  removeTask(sprintId: number, taskId: number) {
    return this.http.delete<ClientResponse<SprintDetailViewModel>>(
      `api/sprints/${sprintId}/tasks/${taskId}`
    );
  }
}
