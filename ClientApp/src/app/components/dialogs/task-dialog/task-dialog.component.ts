import { Component, OnInit, Optional, Inject } from '@angular/core';
import { ProjectTask } from '../../../models/project-task';
import { Project } from '../../../models/project';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { ProjectsService } from '../../../services/projects/projects.service';
import { FormGroup, FormControl, Validators } from '@angular/forms';

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
    public projectsService: ProjectsService) {

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

  ngOnInit() {

    this.projectsService.refreshProjects();

    if (this.task) {

      this.projectFromGroup.controls['nameFormControl'].setValue(this.task.name);
      this.projectFromGroup.controls['projectFormControl'].setValue(this.task.projectId);
      this.projectFromGroup.controls['descriptionFormControl'].setValue(this.task.description);
    } else {
      this.projectFromGroup.reset();
    }
  }

  close(): void {
    this.dialogRef.close();
  }

  getResult() {
    const taskResult = new ProjectTask();

    if (this.task) {
      taskResult.taskId = this.task.taskId;
    }

    taskResult.name = this.projectFromGroup.controls['nameFormControl'].value;
    taskResult.projectId = this.projectFromGroup.controls['projectFormControl'].value;
    taskResult.description = this.projectFromGroup.controls['descriptionFormControl'].value;

    return taskResult;
  }
}
