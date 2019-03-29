import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ProjectTaskDto } from '@app/core/models/view-models/project-task-dto';
import { environment } from '@env/environment';

@Injectable({
  providedIn: 'root',
})
export class ProjectTasksService {
  constructor(private http: HttpClient) {}

  get(workspaceId: number) {
    return this.http.get<ProjectTaskDto[]>(
      environment.apiEndpoint + `api/ProjectTasks?workspaceId=${workspaceId}`
    );
  }
}
