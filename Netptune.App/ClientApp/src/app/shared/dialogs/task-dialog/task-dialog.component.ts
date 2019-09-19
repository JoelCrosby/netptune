import { Component, Inject, OnDestroy, OnInit, Optional } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { AppState } from '@core/core.state';
import { ProjectTask } from '@core/models/project-task';
import { SelectCurrentWorkspace, SelectCurrentProject } from '@core/state/core.selectors';
import { ActionCreateProjectTask } from '@app/features/project-tasks/store/project-tasks.actions';
import { selectProjects } from '@app/features/projects/store/projects.selectors';
import { Store } from '@ngrx/store';
import { Subscription } from 'rxjs';
import { ActionSelectProject } from '@core/state/core.actions';
import { ActionLoadProjects } from '@app/features/projects/store/projects.actions';
import { Project } from '@core/models/project';
import { ProjectTaskStatus } from '@core/enums/project-task-status';

@Component({
  selector: 'app-task-dialog',
  templateUrl: './task-dialog.component.html',
  styleUrls: ['./task-dialog.component.scss'],
})
export class TaskDialogComponent implements OnInit, OnDestroy {
  task: ProjectTask;
  projects$ = this.store.select(selectProjects);
  currentWorkspace$ = this.store.select(SelectCurrentWorkspace);
  currentProject$ = this.store.select(SelectCurrentProject);
  subs = new Subscription();

  showDescriptionField = false;

  selectedTypeValue: number;

  constructor(
    private store: Store<AppState>,
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

  ngOnInit() {
    this.store.dispatch(new ActionLoadProjects());

    if (this.task) {
      this.projectFromGroup.get('nameFormControl').setValue(this.task.name);
      this.projectFromGroup.get('projectFormControl').setValue(this.task.projectId);
      this.projectFromGroup.get('descriptionFormControl').setValue(this.task.description);
    } else {
      this.projectFromGroup.reset();
      this.subs.add(
        this.currentProject$.subscribe(project => {
          this.projectFromGroup.get('projectFormControl').setValue(project);
        })
      );
    }
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }

  close(): void {
    this.dialogRef.close();
  }

  selectProject() {
    const project = this.projectFromGroup.controls['projectFormControl'].value;
    this.store.dispatch(new ActionSelectProject(project));
  }

  getResult() {
    this.subs.add(
      this.currentWorkspace$.subscribe(workspace => {
        const taskResult: ProjectTask = {
          name: this.projectFromGroup.controls['nameFormControl'].value,
          project: this.projectFromGroup.controls['projectFormControl'].value,
          description: this.projectFromGroup.controls['descriptionFormControl'].value,
          workspace: workspace,
          workspaceId: workspace.id,
          projectId: (this.projectFromGroup.controls['projectFormControl'].value as Project).id,
          assigneeId: undefined,
          assignee: undefined,
          ownerId: undefined,
          owner: undefined,
          id: undefined,
          status: ProjectTaskStatus.New,
        };

        this.store.dispatch(new ActionCreateProjectTask(taskResult));

        this.dialogRef.close();
      })
    );
  }
}
