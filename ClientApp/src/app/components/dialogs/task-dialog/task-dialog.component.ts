import { Component, Inject, OnInit, Optional } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { Project } from '../../../models/project';
import { ProjectTask } from '../../../models/project-task';
import { ProjectsService } from '../../../services/projects/projects.service';

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

  async ngOnInit() {

    await this.projectsService.refreshProjects();

    if (this.task) {

      this.projectFromGroup.controls['nameFormControl'].setValue(this.task.name);
      this.projectFromGroup.controls['projectFormControl'].setValue(this.task.projectId);
      this.projectFromGroup.controls['descriptionFormControl'].setValue(this.task.description);
    } else {
      this.projectFromGroup.reset();
      this.projectFromGroup.controls['projectFormControl'].setValue(this.projectsService.currentProject.id);
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
    taskResult.project = this.projectsService.projects.find(x => x.id === taskResult.projectId);
    taskResult.description = this.projectFromGroup.controls['descriptionFormControl'].value;

    return taskResult;
  }
}
