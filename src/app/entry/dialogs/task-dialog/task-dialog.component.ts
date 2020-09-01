import {
  ChangeDetectionStrategy,
  Component,
  Inject,
  OnDestroy,
  OnInit,
  Optional,
} from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { ProjectViewModel } from '@app/core/models/view-models/project-view-model';
import { Workspace } from '@app/core/models/workspace';
import { TaskStatus } from '@core/enums/project-task-status';
import { Project } from '@core/models/project';
import { AddProjectTaskRequest, ProjectTask } from '@core/models/project-task';
import { selectProject } from '@core/store/core/core.actions';
import { loadProjects } from '@core/store/projects/projects.actions';
import * as ProjectSelectors from '@core/store/projects/projects.selectors';
import { createProjectTask } from '@core/store/tasks/tasks.actions';
import * as WorkspaceSelectors from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { Observable, Subject } from 'rxjs';
import { first, takeUntil } from 'rxjs/operators';

@Component({
  selector: 'app-task-dialog',
  templateUrl: './task-dialog.component.html',
  styleUrls: ['./task-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskDialogComponent implements OnInit, OnDestroy {
  task: ProjectTask;
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

  constructor(
    private store: Store,
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
    this.currentWorkspace$ = this.store.select(
      WorkspaceSelectors.selectCurrentWorkspace
    );
    this.currentProject$ = this.store.select(
      ProjectSelectors.selectCurrentProject
    );
    this.projects$ = this.store.select(ProjectSelectors.selectAllProjects);

    this.store.dispatch(loadProjects());

    if (this.task) {
      this.name.setValue(this.task.name);
      this.project.setValue(this.task.projectId);
      this.description.setValue(this.task.description);
    } else {
      this.projectFromGroup.reset();

      this.currentProject$
        .pipe(takeUntil(this.onDestroy$))
        .subscribe((project) => {
          this.project.setValue(project);
        });
    }
  }

  ngOnDestroy() {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  close() {
    this.dialogRef.close();
  }

  selectProject() {
    const project = this.project.value;
    this.store.dispatch(selectProject({ project }));
  }

  saveClicked() {
    this.currentWorkspace$.pipe(first()).subscribe((workspace) => {
      const task: AddProjectTaskRequest = {
        name: (this.name.value as string).trim(),
        description: (this.description.value as string).trim(),
        workspace: workspace.slug,
        projectId: (this.project.value as Project).id,
        assigneeId: undefined,
        assignee: undefined,
        status: TaskStatus.New,
        sortOrder: 0,
      };

      this.store.dispatch(createProjectTask({ task }));

      this.dialogRef.close();
    });
  }
}
