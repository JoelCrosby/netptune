import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatDialog, MatExpansionPanel, MatSnackBar } from '@angular/material';
import { saveAs } from 'file-saver';
import { Subscription } from 'rxjs';
import { fadeIn } from '../../../animations';
import { TaskDialogComponent } from '../../../dialogs/task-dialog/task-dialog.component';
import { ProjectTaskStatus } from '../../../enums/project-task-status';
import { ProjectTask } from '../../../models/project-task';
import { ProjectTaskDto } from '../../../models/view-models/project-task-dto';
import { AuthService } from '../../../services/auth/auth.service';
import { ProjectTaskService } from '../../../services/project-task/project-task.service';
import { ProjectsService } from '../../../services/projects/projects.service';
import { UtilService } from '../../../services/util/util.service';
import { WorkspaceService } from '../../../services/workspace/workspace.service';

@Component({
  selector: 'app-project-tasks',
  templateUrl: './project-tasks.index.component.html',
  styleUrls: ['./project-tasks.index.component.scss'],
  animations: [fadeIn]
})
export class ProjectTasksComponent implements OnInit, OnDestroy {

  exportInProgress = false;

  selectedTask: ProjectTask;
  subs = new Subscription();

  completedStatus = ProjectTaskStatus.Complete;
  inProgressStatus = ProjectTaskStatus.InProgress;
  blockedStatus = ProjectTaskStatus.OnHold;
  backlogStatus = ProjectTaskStatus.InActive;

  myTasks: ProjectTaskDto[] = [];
  completedTasks: ProjectTaskDto[] = [];
  backlogTasks: ProjectTaskDto[] = [];

  completedTasksPeers: string[] = ['myTasks', 'backlogTasks'];
  inProgressTasksPeers: string[] = ['completedTasks', 'backlogTasks'];
  backlogTasksPeers: string[] = ['completedTasks', 'myTasks'];

  taskspanel: MatExpansionPanel;

  dataLoaded = false;

  constructor(
    public projectTaskService: ProjectTaskService,
    private projectsService: ProjectsService,
    private workspaceService: WorkspaceService,
    public snackBar: MatSnackBar,
    private utilService: UtilService,
    private authService: AuthService,
    public dialog: MatDialog
  ) {
    this.subs.add(this.projectTaskService.taskUpdated
      .subscribe(async _ => await this.refreshData())
    );
    this.subs.add(this.projectTaskService.taskAdded
      .subscribe(async _ => await this.refreshData())
    );
    this.subs.add(this.projectTaskService.taskDeleted
      .subscribe(async _ => await this.refreshData())
    );
  }

  ngOnInit(): void {
    this.refreshData();
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }

  async refreshData(): Promise<void> {
    await this.projectTaskService.refreshTasks();

    this.utilService.smoothUpdate(this.myTasks, this.projectTaskService.myTasks);
    this.utilService.smoothUpdate(this.completedTasks, this.projectTaskService.completedTasks);
    this.utilService.smoothUpdate(this.backlogTasks, this.projectTaskService.backlogTasks);

    this.dataLoaded = true;
  }

  async addProjectTask(task: ProjectTask): Promise<void> {
    await this.projectTaskService.addProjectTask(task);
  }

  showAddModal(): void {
    this.open();
  }

  open(): void {
    const dialogRef = this.dialog.open(TaskDialogComponent, {
      width: '600px'
    });

    dialogRef.afterClosed().subscribe((result: ProjectTask) => {
      if (!result) {
        return;
      }

      const workspace = this.workspaceService.currentWorkspace;
      if (!workspace) throw new Error(`current workspace was undefined`);

      const project = this.projectsService.projects.find(x => x.id === result.id);
      if (!project) throw new Error(`unable to find project with id ${result.id}`);

      const newProjectTask = new ProjectTask();
      newProjectTask.name = result.name;
      newProjectTask.description = result.description;
      newProjectTask.projectId = result.projectId;
      newProjectTask.workspaceId = workspace.id;
      newProjectTask.project = project;
      newProjectTask.assigneeId = this.authService.token.userId;

      this.addProjectTask(newProjectTask);
    });
  }

  async exportProjects(): Promise<void> {
    this.exportInProgress = true;

    try {
      const result = this.projectTaskService.getTasks(this.workspaceService.currentWorkspace).toPromise();
      const blob = new Blob([JSON.stringify(result, null, '\t')], { type: 'text/plain;charset=utf-8' });
      saveAs(blob, 'projects.json');
      this.exportInProgress = false;
    } catch (error) {
      this.exportInProgress = error != null;
    }
  }
}
