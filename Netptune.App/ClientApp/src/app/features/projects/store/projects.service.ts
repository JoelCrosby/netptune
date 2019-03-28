import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Project } from '@app/core/models/project';
import { environment } from '@env/environment';

@Injectable({
  providedIn: 'root',
})
export class ProjectsService {
  constructor(private http: HttpClient) {}

  get(workspaceId: number) {
    return this.http.get<Project[]>(
      environment.apiEndpoint + `api/projects?workspaceId=${workspaceId}`
    );
  }
}
