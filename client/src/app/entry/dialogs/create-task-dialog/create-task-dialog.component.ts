import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
} from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { AppState } from '@core/core.state';
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
  projects$!: Observable<ProjectViewModel[]>;
  currentWorkspace$!: Observable<Workspace | undefined>;

  selectedTypeValue!: number;

  formGroup = new FormGroup({
    name: new FormControl('', [Validators.required, Validators.minLength(4)]),
    project: new FormControl<number | null | undefined>(null),
    description: new FormControl(''),
  });

  onDestroy$ = new Subject<void>();

  get name() {
    return this.formGroup.get('name') as FormControl;
  }
  get description() {
    return this.formGroup.get('description') as FormControl;
  }
  get project() {
    return this.formGroup.get('project') as FormControl;
  }

  constructor(
    private store: Store<AppState>,
    public dialogRef: MatDialogRef<CreateTaskDialogComponent>
  ) {}

  ngOnInit() {
    this.projects$ = this.store.select(ProjectSelectors.selectAllProjects);
    this.currentWorkspace$ = this.store.select(
      WorkspaceSelectors.selectCurrentWorkspace
    );

    this.store.select(ProjectSelectors.selectCurrentProjectId).subscribe({
      next: (value) => this.formGroup.controls.project.setValue(value),
    });

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
        projectId: +this.project.value,
        status: TaskStatus.new,
      };

      if (!workspace?.slug) {
        throw new Error('workspace slug is undefined');
      }

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
