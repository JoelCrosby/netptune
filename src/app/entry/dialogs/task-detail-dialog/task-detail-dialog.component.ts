import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  Inject,
  OnDestroy,
  OnInit,
  Optional,
} from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { User } from '@app/core/auth/store/auth.models';
import { selectCurrentUser } from '@app/core/auth/store/auth.selectors';
import { AddCommentRequest } from '@app/core/models/requests/add-comment-request';
import { TaskStatus } from '@core/enums/project-task-status';
import { CommentViewModel } from '@core/models/comment';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import * as ProjectActions from '@core/store/projects/projects.actions';
import * as ProjectSelectors from '@core/store/projects/projects.selectors';
import * as TaskActions from '@core/store/tasks/tasks.actions';
import * as TaskSelectors from '@core/store/tasks/tasks.selectors';
import * as UsersSelectors from '@core/store/users/users.selectors';
import * as UsersActions from '@core/store/users/users.actions';
import { Actions, ofType } from '@ngrx/effects';
import { Action, Store } from '@ngrx/store';
import { Observable, Subject } from 'rxjs';
import { debounceTime, filter, first, takeUntil, tap } from 'rxjs/operators';
import { AppUser } from '@app/core/models/appuser';

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
  users$: Observable<AppUser[]>;
  comments$: Observable<CommentViewModel[]>;
  user$: Observable<User>;

  selectedTypeValue: number;

  onDestroy$ = new Subject();

  constructor(
    public dialogRef: MatDialogRef<TaskDetailDialogComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: TaskViewModel,
    private store: Store,
    private actions$: Actions<Action>
  ) {}

  projectFromGroup: FormGroup;
  commentsFromGroup: FormGroup;

  get name() {
    return this.projectFromGroup.get('name');
  }

  get description() {
    return this.projectFromGroup.get('description');
  }

  get comment() {
    return this.commentsFromGroup.get('comment');
  }

  ngOnInit() {
    this.task$ = this.store.select(TaskSelectors.selectDetailTask).pipe(
      filter((task) => !!task),
      tap((task) => {
        this.buildForm(task);
        this.loadComments(task);
      })
    );

    this.projects$ = this.store.select(ProjectSelectors.selectAllProjects);
    this.comments$ = this.store.select(TaskSelectors.selectComments);
    this.user$ = this.store.select(selectCurrentUser);
    this.users$ = this.store.select(UsersSelectors.selectAllUsers);
  }

  ngAfterViewInit() {
    this.store.dispatch(
      TaskActions.loadTaskDetails({ systemId: this.data.systemId })
    );

    this.store.dispatch(ProjectActions.loadProjects());
    this.store.dispatch(UsersActions.loadUsers());
  }

  buildForm(task: TaskViewModel) {
    this.projectFromGroup = new FormGroup({
      name: new FormControl(task?.name, {
        updateOn: 'blur',
        validators: [Validators.required, Validators.minLength(4)],
      }),
      description: new FormControl(task?.description, {
        updateOn: 'blur',
        validators: [],
      }),
    });

    this.commentsFromGroup = new FormGroup({
      comment: new FormControl('', {
        updateOn: 'change',
      }),
    });

    this.monitorInputs(task);
  }

  monitorInputs(task: TaskViewModel) {
    this.name.valueChanges
      .pipe(
        takeUntil(this.onDestroy$),
        debounceTime(300),
        tap((name) => {
          const updated: TaskViewModel = {
            ...task,
            name,
          };

          this.updateTask(updated);
        })
      )
      .subscribe();

    this.description.valueChanges
      .pipe(
        takeUntil(this.onDestroy$),
        debounceTime(300),
        tap((description) => {
          const updated: TaskViewModel = {
            ...task,
            description,
          };

          this.updateTask(updated);
        })
      )
      .subscribe();
  }

  loadComments(task: TaskViewModel) {
    this.store.dispatch(TaskActions.loadComments({ systemId: task.systemId }));
  }

  onCommentSubmit() {
    const value = this.comment.value as string;

    if (!value) return;

    this.task$
      .pipe(
        first(),
        tap((viewModel) => {
          const request: AddCommentRequest = {
            comment: value.trim(),
            systemId: viewModel.systemId,
            workspaceSlug: viewModel.workspaceSlug,
          };

          this.store.dispatch(TaskActions.addComment({ request }));

          this.comment.reset();
        })
      )
      .subscribe();
  }

  updateTask(task: TaskViewModel) {
    this.store.dispatch(
      TaskActions.editProjectTask({
        task,
      })
    );
  }

  close() {
    this.dialogRef.close();
  }

  ngOnDestroy() {
    this.store.dispatch(TaskActions.clearTaskDetail());
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  onFlagClicked() {
    this.task$
      .pipe(
        first(),
        tap((task) => {
          const updated: TaskViewModel = {
            ...task,
            isFlagged: !task.isFlagged,
          };

          this.updateTask(updated);
        })
      )
      .subscribe();
  }

  selectProject(projectId: number) {
    this.task$
      .pipe(
        first(),
        tap((task) => {
          const updated: TaskViewModel = {
            ...task,
            projectId,
          };

          this.updateTask(updated);
        })
      )
      .subscribe();
  }

  selectAssignee(user: AppUser) {
    this.task$
      .pipe(
        first(),
        tap((task) => {
          const updated: TaskViewModel = {
            ...task,
            assigneeId: user.id,
            assigneePictureUrl: user.pictureUrl,
            assigneeUsername: user.userName,
          };

          this.updateTask(updated);
        })
      )
      .subscribe();
  }

  deleteClicked() {
    this.actions$
      .pipe(
        ofType(TaskActions.deleteProjectTasksSuccess),
        takeUntil(this.onDestroy$),
        first(),
        tap(() => this.dialogRef.close())
      )
      .subscribe();

    this.task$
      .pipe(
        first(),
        tap((task) => {
          this.store.dispatch(TaskActions.deleteProjectTask({ task }));
        })
      )
      .subscribe();
  }
}
