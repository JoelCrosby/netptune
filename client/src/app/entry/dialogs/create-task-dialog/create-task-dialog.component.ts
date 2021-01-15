import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
} from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { TaskStatus } from '@core/enums/project-task-status';
import { AddProjectTaskRequest } from '@core/models/project-task';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { Workspace } from '@core/models/workspace';
import { loadProjects } from '@core/store/projects/projects.actions';
import * as ProjectSelectors from '@core/store/projects/projects.selectors';
import { createProjectTask } from '@core/store/tasks/tasks.actions';
import * as WorkspaceSelectors from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { Observable, Subject } from 'rxjs';
import { first } from 'rxjs/operators';

@Component({
  templateUrl: './create-task-dialog.component.html',
  styleUrls: ['./create-task-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreateTaskDialogComponent implements OnInit, OnDestroy {
  projects$: Observable<ProjectViewModel[]>;
  currentWorkspace$: Observable<Workspace>;
  currentProject$: Observable<ProjectViewModel>;

  selectedTypeValue: number;

  onDestroy$ = new Subject();

  get name() {
    return this.projectFromGroup.get('nameFormControl');
  }
  get description() {
    return this.projectFromGroup.get('descriptionFormControl');
  }
  get project() {
    return this.projectFromGroup.get('projectFormControl');
  }

  projectFromGroup = new FormGroup({
    nameFormControl: new FormControl('', [
      Validators.required,
      Validators.minLength(4),
    ]),
    projectFormControl: new FormControl(),
    descriptionFormControl: new FormControl(),
  });

  constructor(
    private store: Store,
    public dialogRef: MatDialogRef<CreateTaskDialogComponent>
  ) {}

  ngOnInit() {
    this.currentWorkspace$ = this.store.select(
      WorkspaceSelectors.selectCurrentWorkspace
    );
    this.currentProject$ = this.store.select(
      ProjectSelectors.selectCurrentProject
    );
    this.projects$ = this.store.select(ProjectSelectors.selectAllProjects);

    this.store.dispatch(loadProjects());
  }

  ngOnDestroy() {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  close() {
    this.dialogRef.close();
  }

  saveClicked() {
    this.currentWorkspace$.pipe(first()).subscribe((workspace) => {
      const task: AddProjectTaskRequest = {
        name: (this.name.value as string).trim(),
        description: (this.description.value as string)?.trim(),
        projectId: this.project.value,
        assigneeId: undefined,
        assignee: undefined,
        status: TaskStatus.new,
        sortOrder: 0,
      };

      this.store.dispatch(
        createProjectTask({
          identifier: `[workspace] ${workspace.slug}`,
          task,
        })
      );

      this.dialogRef.close();
    });
  }
}
