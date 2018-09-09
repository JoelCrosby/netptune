import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Project } from '../../models/project';
import { Workspace } from '../../models/workspace';
import { AuthService } from '../auth/auth.service';
import { UserService } from '../user/user.service';
import { WorkspaceService } from '../workspace/workspace.service';

@Injectable({
  providedIn: 'root'
})
export class ProjectsService {

  public projects: Project[];

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private workspaceService: WorkspaceService,
    private userService: UserService,
    @Inject('BASE_URL') private baseUrl: string) { }

  refreshProjects(workspace?: Workspace): void {

    this.getProjects(workspace ? workspace : this.workspaceService.currentWorkspace)
      .subscribe((projects: Project[]) => {
        this.projects = projects;
        this.projects.forEach(x => {
          this.userService.getUser(x.ownerId).subscribe(data => x.owner = data);
        });
      });
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

    return this.http.get<Project[]>(this.baseUrl + 'api/Projects' + `?workspaceId=${worspace.workspaceId}`, httpOptions)
      .pipe(
        catchError(this.handleError)
      );
  }

  addProject(project: Project): Observable<Project> {
    const httpOptions = this.getHeaders();

    return this.http.post<Project>(this.baseUrl + 'api/Projects', project, httpOptions)
      .pipe(
        catchError(this.handleError)
      );
  }

  updateProject(project: Project): Observable<Project> {
    const httpOptions = this.getHeaders();

    const url = `${this.baseUrl}api/projects/${project.projectId}`;
    return this.http.put<Project>(url, project, httpOptions)
      .pipe(
        catchError(this.handleError)
      );
  }

  deleteProject(project: Project): Observable<Project> {
    const httpOptions = this.getHeaders();

    const url = `${this.baseUrl}api/projects/${project.projectId}`;
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
