import { Component, Input, OnInit } from '@angular/core';
import { MatDialog, MatSnackBar, MatExpansionPanel } from '@angular/material';
import { dropIn, toggleChip } from '../../../animations';
import { Project } from '../../../models/project';
import { ProjectTask, ProjectTaskStatus } from '../../../models/project-task';
import { AlertService } from '../../../services/alert/alert.service';
import { ProjectTaskService } from '../../../services/project-task/project-task.service';
import { ProjectsService } from '../../../services/projects/projects.service';
import { UserService } from '../../../services/user/user.service';
import { WorkspaceService } from '../../../services/workspace/workspace.service';
import { ConfirmDialogComponent } from '../../dialogs/confirm-dialog/confirm-dialog.component';
import { TaskDialogComponent } from '../../dialogs/task-dialog/task-dialog.component';

@Component({
  selector: 'app-task-list',
  templateUrl: './task-list.component.html',
  styleUrls: ['./task-list.component.scss'],
  animations: [dropIn, toggleChip]
})
export class TaskListComponent implements OnInit {

  @Input() tasks: ProjectTask[];
  @Input() dragGroupName: string;
  @Input() identifier: string;
  @Input() dragExpaneded: boolean;

  public selectedTask: ProjectTask;

  Complete = ProjectTaskStatus.Complete;
  InProgress = ProjectTaskStatus.InProgress;
  Blocked = ProjectTaskStatus.OnHold;

  constructor(
    public dialog: MatDialog,
    public snackBar: MatSnackBar,
    public userService: UserService,
    public projectTaskService: ProjectTaskService,
    private projectsService: ProjectsService,
    private workspaceService: WorkspaceService,
    private alertsService: AlertService
  ) { }

  ngOnInit() { }

  trackById(index: number, task: ProjectTask) {
    return task.id;
  }

  expandPanel(matExpansionPanel: MatExpansionPanel, event: Event): void {
    event.stopPropagation(); // Preventing event bubbling

    if (!this._isExpansionIndicator(event.target)) {
      matExpansionPanel.close(); // Here's the magic
    }
  }

  private _isExpansionIndicator(target: EventTarget): boolean {
    const expansionIndicatorClass = 'mat-expansion-indicator';
    return ((<Element>target).classList && (<Element>target).classList.contains(expansionIndicatorClass));
  }

  clearModalValues(): void {
    // finally clear selecetd project
    this.selectedTask = null;
  }

  refreshData(): void {
    this.projectTaskService.refreshTasks(this.workspaceService.currentWorkspace);
  }

  showUpdateModal(task: ProjectTask): void {
    if (task == null) {
      return;
    }

    this.selectedTask = task;
    this.open(this.selectedTask);
  }

  showDeleteModal(task: ProjectTask): void {
    if (task == null) {
      return;
    }

    this.selectedTask = task;
    this.openConfirmationDialog(this.selectedTask);
  }

  getStatusClass(task: ProjectTask): string {
    switch (task.status) {
      case ProjectTaskStatus.Complete:
        return 'fas fa-check completed';
      case ProjectTaskStatus.InProgress:
        return 'fas fa-minus in-progress';
      case ProjectTaskStatus.OnHold:
        return 'fas fa-minus-circle blocked';
      default:
        return '';
    }
  }

  UpdateSortOrder(): void {
    this.projectTaskService.updateSortOrder(this.tasks).subscribe((responce: ProjectTask[]) => { });
  }

  async statusClicked(task: ProjectTask, status: ProjectTaskStatus): Promise<void> {
    await this.projectTaskService.changeTaskStatus(task, status);
  }

  open(task?: ProjectTask): void {
    const dialogRef = this.dialog.open(TaskDialogComponent, {
      width: '600px',
      data: task
    });

    dialogRef.afterClosed().subscribe(async (result: Project) => {
      if (!result) {
        return;
      }
      const updatedProjectTask = new ProjectTask();
      updatedProjectTask.id = this.selectedTask.id;
      updatedProjectTask.name = result.name;
      updatedProjectTask.description = result.description;
      await this.projectTaskService.updateProjectTask(updatedProjectTask);

      this.clearModalValues();
    });
  }

  openConfirmationDialog(task: ProjectTask): void {
    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '600px',
      data: {
        title: 'Delete Task',
        content: `Are you sure you wish to delete ${task.name}?`,
        confirm: 'Remove'
      }
    });

    dialogRef.afterClosed().subscribe((result: ProjectTask) => {
      if (result) {
        this.deleteProjectTask(task);
      }

      this.clearModalValues();
    });
  }

  async deleteProjectTask(task: ProjectTask): Promise<void> {
    await this.projectTaskService.deleteProjectTask(task);
  }
}
