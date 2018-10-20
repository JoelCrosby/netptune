import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ProjectTask, ProjectTaskStatus } from '../../models/project-task';
import { Workspace } from '../../models/workspace';
import { AuthService } from '../auth/auth.service';
import { UserService } from '../user/user.service';
import { WorkspaceService } from '../workspace/workspace.service';

@Injectable({
  providedIn: 'root'
})
export class ProjectTaskService {

  public tasks: ProjectTask[] = [];

  public myTasks: ProjectTask[] = [];
  public completedTasks: ProjectTask[] = [];
  public backlogTasks: ProjectTask[] = [];

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private workspaceService: WorkspaceService,
    private userService: UserService,
    @Inject('BASE_URL') private baseUrl: string) {
    this.authService.onLogout.subscribe(() => {
      this.tasks = [];
    });
  }

  refreshTasks(workspace = this.workspaceService.currentWorkspace): void {
    this.getTasks(workspace ? workspace : this.workspaceService.currentWorkspace)
      .subscribe((response: ProjectTask[]) => {

        this.tasks.splice(0, this.tasks.length);
        this.tasks.push.apply(this.tasks, response);

        this.myTasks = this.tasks.filter(x => x.assigneeId === this.authService.token.userId && x.status !== ProjectTaskStatus.Complete);
        this.completedTasks = this.tasks.filter(x => x.status === ProjectTaskStatus.Complete);
        this.backlogTasks = this.tasks.filter(X => X.status === ProjectTaskStatus.InActive);

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

  getTasks(worspace: Workspace): Observable<ProjectTask[]> {
    const httpOptions = this.getHeaders();

    return this.http.get<ProjectTask[]>(this.baseUrl + 'api/ProjectTasks' + '?workspaceId=' + worspace.workspaceId, httpOptions)
      .pipe(
        catchError(this.handleError)
      );
  }

  addTask(task: ProjectTask): Observable<ProjectTask> {
    const httpOptions = this.getHeaders();

    if (!task.workspace) {
      task.workspace = this.workspaceService.currentWorkspace;
    }

    return this.http.post<ProjectTask>(this.baseUrl + 'api/ProjectTasks', task, httpOptions)
      .pipe(
        catchError(this.handleError)
      );
  }

  updateTask(task: ProjectTask): Observable<ProjectTask> {
    const httpOptions = this.getHeaders();

    const url = `${this.baseUrl}api/ProjectTasks/${task.projectTaskId}`;
    return this.http.put<ProjectTask>(url, task, httpOptions)
      .pipe(
        catchError(this.handleError)
      );
  }

  deleteTask(task: ProjectTask): Observable<ProjectTask> {
    const httpOptions = this.getHeaders();

    const url = `${this.baseUrl}api/ProjectTasks/${task.projectTaskId}`;
    return this.http.delete<ProjectTask>(url, httpOptions)
      .pipe(
        catchError(this.handleError)
      );
  }

  updateSortOrder(tasks: ProjectTask[]): Observable<ProjectTask[]> {
    const httpOptions = this.getHeaders();

    const url = `${this.baseUrl}api/ProjectTasks/UpdateSortOrder`;
    return this.http.post<ProjectTask[]>(url, tasks, httpOptions)
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
