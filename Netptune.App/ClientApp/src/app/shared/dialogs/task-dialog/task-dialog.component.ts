import { Component, Inject, OnDestroy, OnInit, Optional } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { createProjectTask } from '@app/features/project-tasks/store/tasks.actions';
import { loadProjects } from '@app/features/projects/store/projects.actions';
import {
  selectAllProjects,
  selectCurrentProject,
} from '@app/features/projects/store/projects.selectors';
import { AppState } from '@core/core.state';
import { TaskStatus } from '@core/enums/project-task-status';
import { Project } from '@core/models/project';
import { ProjectTask, AddProjectTaskRequest } from '@core/models/project-task';
import { selectProject } from '@core/state/core.actions';
import { Store } from '@ngrx/store';
import { Subscription } from 'rxjs';
import { SelectCurrentWorkspace } from '@app/core/workspaces/workspaces.selectors';

@Component({
  selector: 'app-task-dialog',
  templateUrl: './task-dialog.component.html',
  styleUrls: ['./task-dialog.component.scss'],
})
export class TaskDialogComponent implements OnInit, OnDestroy {
  task: ProjectTask;
  projects$ = this.store.select(selectAllProjects);
  currentWorkspace$ = this.store.select(SelectCurrentWorkspace);
  currentProject$ = this.store.select(selectCurrentProject);
  subs = new Subscription();

  showDescriptionField = false;

  selectedTypeValue: number;

  get name() {
    return this.projectFromGroup.get('nameFormControl');
  }
  get description() {
    return this.projectFromGroup.get('descriptionFormControl');
  }
  get project() {
    return this.projectFromGroup.get('projectFormControl');
  }

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
    nameFormControl: new FormControl('', [
      Validators.required,
      Validators.minLength(4),
    ]),
    projectFormControl: new FormControl(),
    descriptionFormControl: new FormControl(),
  });

  ngOnInit() {
    this.store.dispatch(loadProjects());

    if (this.task) {
      this.name.setValue(this.task.name);
      this.project.setValue(this.task.projectId);
      this.description.setValue(this.task.description);
    } else {
      this.projectFromGroup.reset();
      this.subs.add(
        this.currentProject$.subscribe(project => {
          this.project.setValue(project);
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
    const project = this.project.value;
    this.store.dispatch(selectProject({ project }));
  }

  getResult() {
    this.subs.add(
      this.currentWorkspace$.subscribe(workspace => {
        const task: AddProjectTaskRequest = {
          name: this.name.value,
          description: this.description.value,
          workspace: workspace.slug,
          projectId: (this.project.value as Project).id,
          assigneeId: undefined,
          assignee: undefined,
          status: TaskStatus.New,
          sortOrder: 0,
        };

        this.store.dispatch(createProjectTask({ task }));

        this.dialogRef.close();
      })
    );
  }
}
