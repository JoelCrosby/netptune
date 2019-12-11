import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ProjectTaskDto } from '@core/models/view-models/project-task-dto';
import { environment } from '@env/environment';
import { ProjectTask } from '@core/models/project-task';
import { throwError } from 'rxjs';
import { Workspace } from '@core/models/workspace';

@Injectable({
  providedIn: 'root',
})
export class ProjectTasksService {
  constructor(private http: HttpClient) {}

  get(workspace: Workspace) {
    if (!workspace) {
      return throwError('no current workspace');
    }
    return this.http.get<ProjectTaskDto[]>(
      environment.apiEndpoint + `api/ProjectTasks?workspaceId=${workspace.id}`
    );
  }

  post(task: ProjectTask) {
    return this.http.post<ProjectTaskDto>(
      environment.apiEndpoint + `api/ProjectTasks`,
      task
    );
  }

  put(task: ProjectTask) {
    return this.http.put<ProjectTaskDto>(
      environment.apiEndpoint + `api/ProjectTasks`,
      task
    );
  }

  delete(task: ProjectTask) {
    return this.http.delete<number>(
      environment.apiEndpoint + `api/ProjectTasks/${task.id}`
    );
  }
}
