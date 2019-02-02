import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Project } from '../../models/project';
import { Workspace } from '../../models/workspace';
import { AuthService } from '../auth/auth.service';
import { WorkspaceService } from '../workspace/workspace.service';
import { Maybe } from '../../core/nothing';

@Injectable({
  providedIn: 'root'
})
export class ProjectsService {

  projects: Project[] = [];
  currentProject: Maybe<Project>;

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private workspaceService: WorkspaceService) {
    this.authService.onLogout.subscribe(() => {
      this.projects = [];
    });
  }

  async refreshProjects(workspace?: Workspace): Promise<void> {

    let w = workspace;

    if (!workspace && this.workspaceService.currentWorkspace) {
      w = this.workspaceService.currentWorkspace;
    }

    if (!w) {
      throw new Error('Unable to determine worksapce to refresh projects for.');
    }

    const response = await this.getProjects(workspace ? workspace : w).toPromise();

    this.projects.splice(0, this.projects.length);
    this.projects.push(...response);

    this.currentProject = this.projects.length > 0 ? this.projects[0] : null;
  }

  getHeaders() {
    return {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ' + this.authService.token.token
      })
    };
  }

  getProjects(workspace: Workspace): Observable<Project[]> {
    const httpOptions = this.getHeaders();

    return this.http.get<Project[]>(environment.apiEndpoint + 'api/Projects' + `?workspaceId=${workspace.id}`, httpOptions);
  }

  addProject(project: Project): Observable<Project> {
    const httpOptions = this.getHeaders();

    return this.http.post<Project>(environment.apiEndpoint + 'api/Projects', project, httpOptions);
  }

  updateProject(project: Project): Observable<Project> {
    const httpOptions = this.getHeaders();

    const url = `${environment.apiEndpoint}api/projects/${project.id}`;
    return this.http.put<Project>(url, project, httpOptions);
  }

  deleteProject(project: Project): Observable<Project> {
    const httpOptions = this.getHeaders();

    const url = `${environment.apiEndpoint}api/projects/${project.id}`;
    return this.http.delete<Project>(url, httpOptions);
  }

}
