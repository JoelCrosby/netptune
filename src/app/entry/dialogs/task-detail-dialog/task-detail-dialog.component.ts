import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  Inject,
  OnInit,
  Optional,
  OnDestroy,
} from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { AppState } from '@core/core.state';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import * as ProjectActions from '@core/projects/projects.actions';
import * as ProjectSelectors from '@core/projects/projects.selectors';
import * as TaskActions from '@project-tasks/store/tasks.actions';
import * as TaskSelectors from '@project-tasks/store/tasks.selectors';
import { Store } from '@ngrx/store';
import { Observable, from, concat, combineLatest } from 'rxjs';
import { tap, first, withLatestFrom, map, filter } from 'rxjs/operators';
import { TaskStatus } from '@core/enums/project-task-status';
import { SelectCurrentWorkspace } from '@core/workspaces/workspaces.selectors';
import { Workspace } from '@core/models/workspace';
import { Comment } from '@core/models/comment';

@Component({
  selector: 'app-task-detail-dialog',
  templateUrl: './task-detail-dialog.component.html',
  styleUrls: ['./task-detail-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskDetailDialogComponent
  implements OnInit, OnDestroy, AfterViewInit {
  task$: Observable<TaskViewModel>;
  projects$: Observable<ProjectViewModel[]>;
  comments$: Observable<Comment[]>;

  selectedTypeValue: number;

  constructor(
    public dialogRef: MatDialogRef<TaskDetailDialogComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: TaskViewModel,
    private store: Store<AppState>
  ) {}

  projectFromGroup: FormGroup;

  ngOnInit() {
    this.task$ = this.store.select(TaskSelectors.selectDetailTask).pipe(
      tap((task) => {
        if (!task) return;

        this.buildForm(task);
        this.loadComments(task);
      })
    );

    this.projects$ = this.store.select(ProjectSelectors.selectAllProjects);
  }

  ngAfterViewInit() {
    this.store.dispatch(
      TaskActions.loadTaskDetails({ systemId: this.data.systemId })
    );

    this.store.dispatch(ProjectActions.loadProjects());
  }

  buildForm(task: TaskViewModel) {
    this.projectFromGroup = new FormGroup({
      nameFormControl: new FormControl(task?.name, [
        Validators.required,
        Validators.minLength(4),
      ]),
      projectFormControl: new FormControl(task?.projectId),
      descriptionFormControl: new FormControl(task?.description),
    });
  }

  loadComments(task: TaskViewModel) {
    const options = {
      headers: {
        Authorization:
          'Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiam9lbGNyb3NieUBsaXZlLmNvLnVrIiwiaHR0cDovL3NjaGVtYXMueG1sc29hcC5vcmcvd3MvMjAwNS8wNS9pZGVudGl0eS9jbGFpbXMvbmFtZWlkZW50aWZpZXIiOiJkZTFhNDE0My05M2M5LTQzZTEtOGMwMy05MGVlNWFkODBmZWQiLCJleHAiOjE1OTMyOTIxMTksImlzcyI6Ik5ldHB0dW5lLmNvbSIsImF1ZCI6Ik5ldHB0dW5lLmNvbSJ9.F8c2knl6sn4QnuIIlVZGtz2te2-OakrUHEb82Pj55pY',
      },
    };

    this.comments$ = from(
      fetch(
        `/api/comments/${task.systemId}?workspace=${task.workspaceSlug}`,
        options
      ).then((res) => res.json())
    );
  }

  close() {
    this.dialogRef.close();
  }

  ngOnDestroy() {
    this.store.dispatch(TaskActions.clearTaskDetail());
  }

  getTaskStatus(status: TaskStatus) {
    return TaskStatus[status];
  }

  onFlagClicked() {
    this.task$
      .pipe(
        first(),
        tap((viewModel) => {
          const task: TaskViewModel = {
            ...viewModel,
            isFlagged: !viewModel.isFlagged,
          };

          this.store.dispatch(
            TaskActions.editProjectTask({
              task,
            })
          );
        })
      )
      .subscribe();
  }
}
