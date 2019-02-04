import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material';
import { Observable, Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Maybe } from '../../core/nothing';
import { ProjectTaskStatus } from '../../enums/project-task-status';
import { ProjectTask } from '../../models/project-task';
import { ProjectTaskCounts } from '../../models/view-models/project-task-counts';
import { ProjectTaskDto } from '../../models/view-models/project-task-dto';
import { Workspace } from '../../models/workspace';
import { AuthService } from '../auth/auth.service';
import { UtilService } from '../util/util.service';
import { WorkspaceService } from '../workspace/workspace.service';

@Injectable({
  providedIn: 'root'
})
export class ProjectTaskService {

  tasks: ProjectTaskDto[] = [];

  myTasks: ProjectTaskDto[] = [];
  completedTasks: ProjectTaskDto[] = [];
  backlogTasks: ProjectTaskDto[] = [];

  taskAdded = new Subject<ProjectTask>();
  taskUpdated = new Subject<ProjectTask>();
  taskDeleted = new Subject<ProjectTask>();

  constructor(
    private http: HttpClient,
    private authService: AuthService,
    private workspaceService: WorkspaceService,
    private snackBar: MatSnackBar,
    private utilService: UtilService
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

  getTasks(workspace: Maybe<Workspace> = this.workspaceService.currentWorkspace): Observable<ProjectTaskDto[]> {
    const httpOptions = this.getHeaders();

    if (!workspace) {
      throw new Error('worksapce supplied to getTasks was undefined');
    }

    return this.http
      .get<ProjectTaskDto[]>(environment.apiEndpoint + 'api/ProjectTasks' + '?workspaceId=' + workspace.id, httpOptions);
  }

  private addTask(task: ProjectTask): Observable<ProjectTask> {
    const httpOptions = this.getHeaders();

    if (!task.workspace && this.workspaceService.currentWorkspace) {
      task.workspace = this.workspaceService.currentWorkspace;
    }

    return this.http.post<ProjectTask>(environment.apiEndpoint + 'api/ProjectTasks', task, httpOptions);
  }

  async addProjectTask(task: ProjectTask): Promise<ProjectTask | undefined> {

    try {

      const result = await this.addTask(task).toPromise();

      if (result) {
        this.taskAdded.next(task);
        this.snackBar.open(`Project ${task.name} Added!.`, undefined, {
          duration: 3000
        });
        return result;
      }
    } catch {
      this.snackBar.open('An error occured while trying to create the task', undefined, {
        duration: 2000
      });
    }

    return undefined;
  }

  private updateTask(task: ProjectTask): Observable<ProjectTask> {
    const httpOptions = this.getHeaders();

    const url = `${environment.apiEndpoint}api/ProjectTasks/${task.id}`;
    return this.http.put<ProjectTask>(url, task, httpOptions);
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
      this.snackBar.open('An error occured while trying to update the task', undefined, {
        duration: 2000
      });
    }
  }

  async deleteProjectTask(task: ProjectTask): Promise<ProjectTask | undefined> {
    try {
      const deletedTask = await this.deleteTask(task).toPromise();
      this.snackBar.open('Task Deleted', undefined, {
        duration: 2000
      });
      this.taskDeleted.next(deletedTask);
      return deletedTask;
    } catch (error) {
      this.snackBar.open('An error occured while trying to delete Task' + error, undefined, {
        duration: 2000
      });
    }
    return undefined;
  }

  private deleteTask(task: ProjectTask): Observable<ProjectTask> {
    const httpOptions = this.getHeaders();

    const url = `${environment.apiEndpoint}api/ProjectTasks/${task.id}`;
    return this.http.delete<ProjectTask>(url, httpOptions);
  }

  public getProjectTaskCount(projectId: number): Observable<ProjectTaskCounts> {
    const httpOptions = this.getHeaders();

    const url = `${environment.apiEndpoint}api/ProjectTasks/GetProjectTaskCount?projectId=${projectId}`;
    return this.http.get<ProjectTaskCounts>(url, httpOptions);
  }

  updateSortOrder(tasks: ProjectTask[]): Observable<ProjectTask[]> {
    const httpOptions = this.getHeaders();

    const url = `${environment.apiEndpoint}api/ProjectTasks/UpdateSortOrder`;
    return this.http.post<ProjectTask[]>(url, tasks, httpOptions);
  }

}
