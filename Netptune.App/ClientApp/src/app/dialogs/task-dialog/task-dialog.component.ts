import { Component, Inject, OnInit, Optional } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { Project } from '../../models/project';
import { ProjectTask } from '../../models/project-task';
import { ProjectsService } from '../../services/projects/projects.service';
import { Maybe } from '../../core/nothing';
import { WorkspaceService } from '../../services/workspace/workspace.service';
import { AuthService } from '../../services/auth/auth.service';

@Component({
  selector: 'app-task-dialog',
  templateUrl: './task-dialog.component.html',
  styleUrls: ['./task-dialog.component.scss']
})
export class TaskDialogComponent implements OnInit {

  public task: ProjectTask;
  public projects: Project[];

  public selectedTypeValue: number;

  constructor(
    public dialogRef: MatDialogRef<TaskDialogComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: ProjectTask,
    public projectsService: ProjectsService,
    private workspaceService: WorkspaceService,
    private authService: AuthService) {

    if (data) {
      this.task = data;
    }
  }

  projectFromGroup = new FormGroup({

    nameFormControl: new FormControl('', [
      Validators.required,
      Validators.minLength(4),
    ]),

    projectFormControl: new FormControl('', [
    ]),

    descriptionFormControl: new FormControl('', [
    ])

  });

  async ngOnInit() {

    await this.projectsService.refreshProjects();

    if (this.task) {

      this.projectFromGroup.controls['nameFormControl'].setValue(this.task.name);
      this.projectFromGroup.controls['projectFormControl'].setValue(this.task.projectId);
      this.projectFromGroup.controls['descriptionFormControl'].setValue(this.task.description);
    } else {
      this.projectFromGroup.reset();
      if (this.projectsService.currentProject) {
        this.projectFromGroup.controls['projectFormControl'].setValue(this.projectsService.currentProject.id);
      }
    }
  }

  close(): void {
    this.dialogRef.close();
  }

  getResult() {

    const taskResult = new ProjectTask();

    if (this.task) {
      taskResult.id = this.task.id;
    }

    taskResult.name = this.projectFromGroup.controls['nameFormControl'].value;
    taskResult.projectId = this.projectFromGroup.controls['projectFormControl'].value;
    taskResult.description = this.projectFromGroup.controls['descriptionFormControl'].value;

    const project = this.projectsService.projects.find(x => x.id === taskResult.projectId);
    if (!project) { throw new Error(`unable to find project with id ${taskResult.projectId}`); }

    taskResult.project = project;

    const workspace = this.workspaceService.currentWorkspace;
    if (!workspace) { throw new Error(`current workspace was undefined`); }

    taskResult.workspaceId = workspace.id;

    const assigneeId = this.authService.token.userId;
    if (!workspace) { throw new Error(`current userId was undefined`); }

    taskResult.assigneeId = assigneeId;

    this.dialogRef.close(taskResult);
  }
}
