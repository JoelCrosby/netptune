import { HttpClient, HttpErrorResponse, HttpHeaders } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material';
import { Observable, throwError, Subject } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ProjectTask } from '../../models/project-task';
import { Workspace } from '../../models/workspace';
import { AuthService } from '../auth/auth.service';
import { UtilService } from '../util/util.service';
import { WorkspaceService } from '../workspace/workspace.service';
import { ProjectTaskStatus } from '../../enums/project-task-status';
import { ProjectTaskDto } from '../../models/view-models/project-task-dto';

@Injectable({
  providedIn: 'root'
})
export class ProjectTaskService {

  public tasks: ProjectTaskDto[] = [];

  public myTasks: ProjectTaskDto[] = [];
  public completedTasks: ProjectTaskDto[] = [];
  public backlogTasks: ProjectTaskDto[] = [];

  public taskAdded = new Subject<ProjectTask>();
  public taskUpdated = new Subject<ProjectTask>();
  public taskDeleted = new Subject<ProjectTask>();

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private workspaceService: WorkspaceService,
    private snackBar: MatSnackBar,
    private utilService: UtilService,
    @Inject('BASE_URL') private baseUrl: string
  ) {
    this.authService.onLogout.subscribe(() => {
      this.tasks = [];
    });
  }

  async refreshTasks(workspace = this.workspaceService.currentWorkspace): Promise<void> {
    const response = await this.getTasks().toPromise();

    this.tasks.splice(0, this.tasks.length);
    this.tasks.push(...response);

    this.utilService.smoothUpdate(
      this.myTasks,
      this.tasks.filter(
        x =>
          x.assigneeId === this.authService.token.userId &&
          x.status !== ProjectTaskStatus.Complete &&
          x.status !== ProjectTaskStatus.InActive
      )
    );
    this.utilService.smoothUpdate(
      this.backlogTasks,
      this.tasks.filter(X => X.status === ProjectTaskStatus.InActive)
    );
    this.utilService.smoothUpdate(
      this.completedTasks,
      this.tasks.filter(x => x.status === ProjectTaskStatus.Complete)
    );

  }

  getHeaders() {
    return {
      headers: new HttpHeaders({
        'Content-Type': 'application/json',
        Authorization: 'Bearer ' + this.authService.token.token
      })
    };
  }

  getTasks(worspace: Workspace = this.workspaceService.currentWorkspace): Observable<ProjectTaskDto[]> {
    const httpOptions = this.getHeaders();

    return this.http
      .get<ProjectTaskDto[]>(this.baseUrl + 'api/ProjectTasks' + '?workspaceId=' + worspace.id, httpOptions);
  }

  private addTask(task: ProjectTask): Observable<ProjectTask> {
    const httpOptions = this.getHeaders();

    if (!task.workspace) {
      task.workspace = this.workspaceService.currentWorkspace;
    }

    return this.http.post<ProjectTask>(this.baseUrl + 'api/ProjectTasks', task, httpOptions).pipe(catchError(this.handleError));
  }

  async addProjectTask(task: ProjectTask): Promise<ProjectTask> {

    try {

      const result = await this.addTask(task).toPromise();

      if (result) {
        this.taskAdded.next(task);
        this.snackBar.open(`Project ${task.name} Added!.`, null, {
          duration: 3000
        });
        return result;
      }
    } catch {
      this.snackBar.open('An error occured while trying to create the task', null, {
        duration: 2000
      });
    }
  }

  private updateTask(task: ProjectTask): Observable<ProjectTask> {
    const httpOptions = this.getHeaders();

    const url = `${this.baseUrl}api/ProjectTasks/${task.id}`;
    return this.http.put<ProjectTask>(url, task, httpOptions).pipe(catchError(this.handleError));
  }

  async changeTaskStatus(task: ProjectTask, staus: ProjectTaskStatus): Promise<void> {
    console.assert(task.status !== staus, 'invalid task update request');
    task.status = staus;
    await this.updateProjectTask(task);
  }

  async updateProjectTask(task: ProjectTask): Promise<void> {
    try {
      await this.updateTask(task).toPromise();
      this.taskUpdated.next(task);
    } catch (error) {
      this.snackBar.open('An error occured while trying to update the task', null, {
        duration: 2000
      });
    }
  }

  async deleteProjectTask(task: ProjectTask): Promise<ProjectTask> {
    try {
      const deletedTask = await this.deleteTask(task).toPromise();
      this.snackBar.open('Task Deleted', null, {
        duration: 2000
      });
      this.taskDeleted.next(deletedTask);
      return deletedTask;
    } catch (error) {
      this.snackBar.open('An error occured while trying to delete Task' + error, null, {
        duration: 2000
      });
    }
    return null;
  }

  private deleteTask(task: ProjectTask): Observable<ProjectTask> {
    const httpOptions = this.getHeaders();

    const url = `${this.baseUrl}api/ProjectTasks/${task.id}`;
    return this.http.delete<ProjectTask>(url, httpOptions).pipe(catchError(this.handleError));
  }

  updateSortOrder(tasks: ProjectTask[]): Observable<ProjectTask[]> {
    const httpOptions = this.getHeaders();

    const url = `${this.baseUrl}api/ProjectTasks/UpdateSortOrder`;
    return this.http.post<ProjectTask[]>(url, tasks, httpOptions).pipe(catchError(this.handleError));
  }

  private handleError(error: HttpErrorResponse) {
    if (error.error instanceof ErrorEvent) {
      // A client-side or network error occurred. Handle it accordingly.
      console.error('An error occurred:', error.error.message);
    } else {
      // The backend returned an unsuccessful response code.
      // The response body may contain clues as to what went wrong,
      console.error(`Backend returned code ${error.status}, ` + `body was: ${error.error}`);
    }
    // return an observable with a user-facing error message
    return throwError('Something bad happened; please try again later.');
  }
}
