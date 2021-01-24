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
import { UserResponse } from '@core/auth/store/auth.models';
import { selectCurrentUser } from '@core/auth/store/auth.selectors';
import { AppUser } from '@core/models/appuser';
import { CommentViewModel } from '@core/models/comment';
import { AddCommentRequest } from '@core/models/requests/add-comment-request';
import { AddTagToTaskRequest } from '@core/models/requests/add-tag-request';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { selectCurrentHubGroupId } from '@core/store/hub-context/hub-context.selectors';
import * as ProjectActions from '@core/store/projects/projects.actions';
import * as ProjectSelectors from '@core/store/projects/projects.selectors';
import * as TagsActions from '@core/store/tags/tags.actions';
import * as TagsSelectors from '@core/store/tags/tags.selectors';
import * as TaskActions from '@core/store/tasks/tasks.actions';
import * as TaskSelectors from '@core/store/tasks/tasks.selectors';
import * as UsersActions from '@core/store/users/users.actions';
import * as UsersSelectors from '@core/store/users/users.selectors';
import { Actions, ofType } from '@ngrx/effects';
import { Action, Store } from '@ngrx/store';
import { AutocompleteChipsSelectionChanged } from '@static/components/autocomplete-chips/autocomplete-chips.component';
import { Observable, Subject } from 'rxjs';
import {
  debounceTime,
  filter,
  first,
  map,
  share,
  takeUntil,
  tap,
  withLatestFrom,
} from 'rxjs/operators';

@Component({
  selector: 'app-task-detail-dialog',
  templateUrl: './task-detail-dialog.component.html',
  styleUrls: ['./task-detail-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskDetailDialogComponent
  implements OnInit, OnDestroy, AfterViewInit {
  static width = '972px';

  task$: Observable<TaskViewModel>;
  projects$: Observable<ProjectViewModel[]>;
  users$: Observable<AppUser[]>;
  comments$: Observable<CommentViewModel[]>;
  user$: Observable<UserResponse>;
  tags$: Observable<string[]>;

  selectedTypeValue: number;

  onDestroy$ = new Subject();
  onEditorLoadedSubject = new Subject<boolean>();
  onEditorLoaded$ = this.onEditorLoadedSubject.pipe();

  projectFromGroup: FormGroup;

  constructor(
    public dialogRef: MatDialogRef<TaskDetailDialogComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: TaskViewModel,
    private store: Store,
    private actions$: Actions<Action>
  ) {}

  get name() {
    return this.projectFromGroup.get('name');
  }

  get description() {
    return this.projectFromGroup.get('description');
  }

  ngOnInit() {
    this.task$ = this.store.select(TaskSelectors.selectDetailTask).pipe(
      filter((task) => !!task),
      tap((task) => {
        this.buildForm(task);
        this.loadComments(task);
      }),
      share()
    );

    this.projects$ = this.store.select(ProjectSelectors.selectAllProjects);
    this.comments$ = this.store.select(TaskSelectors.selectComments);
    this.tags$ = this.store.select(TagsSelectors.selectTagNames);

    this.user$ = this.store.select(selectCurrentUser);
    this.users$ = this.store.select(UsersSelectors.selectAllUsers).pipe(
      withLatestFrom(this.getTaskObservable()),
      map(([users, task]) => users.filter((u) => u.id !== task.assigneeId))
    );
  }

  ngAfterViewInit() {
    this.store.dispatch(
      TaskActions.loadTaskDetails({ systemId: this.data.systemId })
    );

    this.store.dispatch(ProjectActions.loadProjects());
    this.store.dispatch(UsersActions.loadUsers());
    this.store.dispatch(TagsActions.loadTags());
  }

  buildForm(task: TaskViewModel) {
    this.projectFromGroup = new FormGroup({
      name: new FormControl(task?.name, {
        updateOn: 'blur',
        validators: [Validators.required, Validators.minLength(4)],
      }),
      description: new FormControl(task?.description, {
        updateOn: 'change',
        validators: [],
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

  getTaskObservable() {
    return this.store.select(TaskSelectors.selectDetailTask);
  }

  loadComments(task: TaskViewModel) {
    this.store.dispatch(TaskActions.loadComments({ systemId: task.systemId }));
  }

  onCommentSubmit(value: string) {
    if (!value) return;

    this.getTaskObservable()
      .pipe(
        first(),
        tap((viewModel) => {
          const request: AddCommentRequest = {
            comment: value.trim(),
            systemId: viewModel.systemId,
          };

          this.store.dispatch(TaskActions.addComment({ request }));
        })
      )
      .subscribe();
  }

  updateTask(task: TaskViewModel) {
    this.store
      .select(selectCurrentHubGroupId)
      .pipe(
        first(),
        tap((identifier) => {
          this.store.dispatch(
            TaskActions.editProjectTask({
              identifier,
              task,
            })
          );
        })
      )
      .subscribe();
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
    this.getTaskObservable()
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
    this.getTaskObservable()
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
    this.getTaskObservable()
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

    this.getTaskObservable()
      .pipe(
        first(),
        withLatestFrom(this.store.select(selectCurrentHubGroupId)),
        tap(([task, identifier]) => {
          this.store.dispatch(
            TaskActions.deleteProjectTask({ identifier, task })
          );
        })
      )
      .subscribe();
  }

  onDeleteCommentClicked(comment: CommentViewModel) {
    const commentId = comment.id;
    this.store.dispatch(TaskActions.deleteComment({ commentId }));
  }

  onTagsSelectionChanged(event: AutocompleteChipsSelectionChanged) {
    this.getTaskObservable()
      .pipe(
        first(),
        withLatestFrom(this.store.select(selectCurrentHubGroupId)),
        tap(([task, identifier]) => {
          if (event.type === 'Removed') {
            const systemId = task.systemId;
            const tag = event.option;

            this.store.dispatch(
              TaskActions.deleteTagFromTask({ identifier, systemId, tag })
            );
          } else {
            const request: AddTagToTaskRequest = {
              systemId: task.systemId,
              tag: event.option,
            };

            this.store.dispatch(
              TaskActions.addTagToTask({ identifier, request })
            );
          }
        })
      )
      .subscribe();
  }

  onEditorLoaded() {
    this.onEditorLoadedSubject.next(true);
  }
}
