import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Project } from '../../models/project';
import { Workspace } from '../../models/workspace';
import { WorkspaceService } from '../workspace/workspace.service';
import { environment } from '../../../environments/environment';
import { AuthService } from '../auth/auth.service';

@Injectable({
  providedIn: 'root'
})
export class ProjectsService {

  public projects: Project[] = [];
  public currentProject: Project;

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private workspaceService: WorkspaceService) {
    this.authService.onLogout.subscribe(() => {
      this.projects = [];
    });
  }

  async refreshProjects(workspace?: Workspace): Promise<void> {

    const response = await this.getProjects(workspace ? workspace : this.workspaceService.currentWorkspace).toPromise();

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

  getProjects(worspace: Workspace): Observable<Project[]> {
    const httpOptions = this.getHeaders();

    return this.http.get<Project[]>(environment.apiEndpoint + 'api/Projects' + `?workspaceId=${worspace.id}`, httpOptions)
      .pipe(
        catchError(this.handleError)
      );
  }

  addProject(project: Project): Observable<Project> {
    const httpOptions = this.getHeaders();

    return this.http.post<Project>(environment.apiEndpoint + 'api/Projects', project, httpOptions)
      .pipe(
        catchError(this.handleError)
      );
  }

  updateProject(project: Project): Observable<Project> {
    const httpOptions = this.getHeaders();

    const url = `${environment.apiEndpoint}api/projects/${project.id}`;
    return this.http.put<Project>(url, project, httpOptions)
      .pipe(
        catchError(this.handleError)
      );
  }

  deleteProject(project: Project): Observable<Project> {
    const httpOptions = this.getHeaders();

    const url = `${environment.apiEndpoint}api/projects/${project.id}`;
    return this.http.delete<Project>(url, httpOptions)
      .pipe(
        catchError(this.handleError)
      );
  }

  private handleError(error: HttpErrorResponse) {
    if (error.error instanceof ErrorEvent) {
      // A client-side or network error occurred. Handle it accordingly.
      console.error('An error occurred:', error.error.message);
    } else {
      // The backend returned an unsuccessful response code.
      // The response body may contain clues as to what went wrong,
      console.error(
        `Backend returned code ${error.status}, ` +
        `body was: ${error.error}`);
    }
    // return an observable with a user-facing error message
    return throwError(
      'Something bad happened; please try again later.');
  }

}
