import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
} from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
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

  selectedTypeValue: number;
  formGroup: FormGroup;

  onDestroy$ = new Subject();

  get name() {
    return this.formGroup.get('name');
  }
  get description() {
    return this.formGroup.get('description');
  }
  get project() {
    return this.formGroup.get('project');
  }

  constructor(
    private fb: FormBuilder,
    private store: Store,
    public dialogRef: MatDialogRef<CreateTaskDialogComponent>
  ) {}

  ngOnInit() {
    this.projects$ = this.store.select(ProjectSelectors.selectAllProjects);
    this.currentWorkspace$ = this.store.select(
      WorkspaceSelectors.selectCurrentWorkspace
    );

    this.store.select(ProjectSelectors.selectCurrentProjectId).subscribe({
      next: (projectId) => {
        this.formGroup = this.fb.group({
          name: ['', [Validators.required, Validators.minLength(4)]],
          project: [projectId],
          description: [],
        });
      },
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
