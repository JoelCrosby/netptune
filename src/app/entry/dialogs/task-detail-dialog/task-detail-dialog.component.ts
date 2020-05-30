import {
  Component,
  Inject,
  OnInit,
  Optional,
  ChangeDetectionStrategy,
} from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Project } from '@core/models/project';
import { ProjectTask } from '@core/models/project-task';

@Component({
  selector: 'app-task-detail-dialog',
  templateUrl: './task-detail-dialog.component.html',
  styleUrls: ['./task-detail-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskDetailDialogComponent implements OnInit {
  task: ProjectTask;
  projects: Project[] = [];

  selectedTypeValue: number;

  constructor(
    public dialogRef: MatDialogRef<TaskDetailDialogComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: ProjectTask
  ) {
    if (data) {
      this.task = data;
    }
  }

  projectFromGroup: FormGroup;

  ngOnInit() {
    this.projectFromGroup = new FormGroup({
      nameFormControl: new FormControl(this.task?.name, [
        Validators.required,
        Validators.minLength(4),
      ]),
      projectFormControl: new FormControl(this.task?.projectId),
      descriptionFormControl: new FormControl(this.task?.description),
    });
  }

  close(): void {
    this.dialogRef.close();
  }

  getResult() {
    const taskResult: ProjectTask = {
      ...this.task,
      name: this.projectFromGroup.get('nameFormControl').value,
      projectId: this.projectFromGroup.get('projectFormControl').value,
      description: this.projectFromGroup.get('descriptionFormControl').value,
    };

    this.dialogRef.close(taskResult);
  }
}
