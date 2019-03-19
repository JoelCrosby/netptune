import { Component, Inject, OnInit, Optional } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { Project } from '@app/core/models/project';
import { ProjectTask } from '@app/core/models/project-task';

@Component({
  selector: 'app-task-dialog',
  templateUrl: './task-dialog.component.html',
  styleUrls: ['./task-dialog.component.scss'],
})
export class TaskDialogComponent implements OnInit {
  task: ProjectTask;
  projects: Project[];

  showDescriptionField = false;

  selectedTypeValue: number;

  constructor(
    public dialogRef: MatDialogRef<TaskDialogComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: ProjectTask
  ) {
    if (data) {
      this.task = data;
    }
  }

  projectFromGroup = new FormGroup({
    nameFormControl: new FormControl('', [Validators.required, Validators.minLength(4)]),
    projectFormControl: new FormControl(),
    descriptionFormControl: new FormControl(),
  });

  async ngOnInit() {
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
      Object.assign(taskResult, this.task);
    }

    taskResult.name = this.projectFromGroup.controls['nameFormControl'].value;
    taskResult.projectId = this.projectFromGroup.controls['projectFormControl'].value;
    taskResult.description = this.projectFromGroup.controls['descriptionFormControl'].value;

    this.dialogRef.close(taskResult);
  }
}
