import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatDialog, MatSnackBar, MatExpansionPanel } from '@angular/material';
import { saveAs } from 'file-saver';
import { DragulaService } from 'ng2-dragula';
import { Subscription, merge } from 'rxjs';
import { Project } from '../../models/project';
import { ProjectTask, ProjectTaskStatus } from '../../models/project-task';
import { ProjectTaskService } from '../../services/project-task/project-task.service';
import { ProjectsService } from '../../services/projects/projects.service';
import { UtilService } from '../../services/util/util.service';
import { WorkspaceService } from '../../services/workspace/workspace.service';
import { TaskDialogComponent } from '../dialogs/task-dialog/task-dialog.component';
import { startWith, mapTo } from 'rxjs/operators';

@Component({
  selector: 'app-project-tasks',
  templateUrl: './project-tasks.component.html',
  styleUrls: ['./project-tasks.component.scss']
})
export class ProjectTasksComponent implements OnInit, OnDestroy {

  public exportInProgress = false;
  public dragGroupName = 'PROJECT_TASKS';

  public selectedTask: ProjectTask;
  public subs = new Subscription();

  public completedStatus = ProjectTaskStatus.Complete;
  public inProgressStatus = ProjectTaskStatus.InProgress;
  public blockedStatus = ProjectTaskStatus.OnHold;
  public backlogStatus = ProjectTaskStatus.InActive;

  public myTasks: ProjectTask[] = [];
  public completedTasks: ProjectTask[] = [];
  public backlogTasks: ProjectTask[] = [];

  public taskspanel: MatExpansionPanel;

  public dragStart$ = this.dragulaService.drag(this.dragGroupName).pipe(mapTo(true));
  public dragEnd$ = this.dragulaService.dragend(this.dragGroupName).pipe(mapTo(false));
  public isDragging$ = merge(this.dragStart$, this.dragEnd$).pipe(startWith(false));

  constructor(
    public projectTaskService: ProjectTaskService,
    private projectsService: ProjectsService,
    private workspaceService: WorkspaceService,
    public snackBar: MatSnackBar,
    private dragulaService: DragulaService,
    private utilService: UtilService,
    public dialog: MatDialog
  ) {
    this.subs.add(
      this.dragulaService
        .dropModel(this.dragGroupName)
        .subscribe(({ el, target, source, item, sourceModel, targetModel, sourceIndex, targetIndex }) => {

          const task = <ProjectTask>item;
          if (!task) { return; }

          if (target.id !== source.id && task.status !== Number(target.id)) {
            this.projectTaskService.changeTaskStatus(task, Number(target.id));
          }
        })
    );
    this.subs.add(this.projectTaskService.taskUpdated
      .subscribe((task: ProjectTask) => this.refreshData())
    );
    this.subs.add(this.projectTaskService.taskAdded
      .subscribe((task: ProjectTask) => this.refreshData())
    );
    this.subs.add(this.projectTaskService.taskDeleted
      .subscribe((task: ProjectTask) => this.refreshData())
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

    dialogRef.afterClosed().subscribe((result: Project) => {
      if (!result) {
        return;
      }

      const newProject = new ProjectTask();
      newProject.name = result.name;
      newProject.description = result.description;
      newProject.projectId = result.projectId;
      newProject.project = this.projectsService.projects.find(x => x.projectId === result.projectId);

      this.addProjectTask(newProject);
    });
  }

  exportProjects(): void {
    this.exportInProgress = true;

    this.projectTaskService.getTasks(this.workspaceService.currentWorkspace).subscribe(
      result => {
        const blob = new Blob([JSON.stringify(result, null, '\t')], { type: 'text/plain;charset=utf-8' });
        saveAs(blob, 'projects.json');
        this.exportInProgress = false;
      },
      error => {
        this.exportInProgress = error != null;
      }
    );
  }
}
