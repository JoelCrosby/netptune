import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ProjectTaskDto } from '@app/core/models/view-models/project-task-dto';
import { environment } from '@env/environment';
import { ProjectTask } from '@app/core/models/project-task';
import { throwError } from 'rxjs';
import { Workspace } from '@app/core/models/workspace';

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
    return this.http.post<ProjectTask>(environment.apiEndpoint + `api/ProjectTasks`, task);
  }
}
