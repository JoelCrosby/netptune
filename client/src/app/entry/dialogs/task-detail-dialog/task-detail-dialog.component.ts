import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  effect,
  inject,
  signal,
} from '@angular/core';
import {
  FormControl,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatIconButton } from '@angular/material/button';
import { MatChipListbox, MatChipOption } from '@angular/material/chips';
import { MatIcon } from '@angular/material/icon';
import { MatMenu, MatMenuItem, MatMenuTrigger } from '@angular/material/menu';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { MatTooltip } from '@angular/material/tooltip';
import { selectRequiredCurrentUser } from '@core/auth/store/auth.selectors';
import { AppUser } from '@core/models/appuser';
import { CommentViewModel } from '@core/models/comment';
import { EntityType } from '@core/models/entity-type';
import { AddCommentRequest } from '@core/models/requests/add-comment-request';
import { AddTagToTaskRequest } from '@core/models/requests/add-tag-request';
import { UpdateProjectTaskRequest } from '@core/models/requests/update-project-task-request';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { selectCurrentHubGroupId } from '@core/store/hub-context/hub-context.selectors';
import * as ProjectSelectors from '@core/store/projects/projects.selectors';
import * as TagsSelectors from '@core/store/tags/tags.selectors';
import * as TaskActions from '@core/store/tasks/tasks.actions';
import * as TaskSelectors from '@core/store/tasks/tasks.selectors';
import * as UsersSelectors from '@core/store/users/users.selectors';
import { ActivityMenuComponent } from '@entry/components/activity-menu/activity-menu.component';
import { Actions, ofType } from '@ngrx/effects';
import { Action, Store } from '@ngrx/store';
import {
  AutocompleteChipsComponent,
  AutocompleteChipsSelectionChanged,
} from '@static/components/autocomplete-chips/autocomplete-chips.component';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { CommentsListComponent } from '@static/components/comments-list/comments-list.component';
import { EditorComponent } from '@static/components/editor/editor.component';
import { InlineTextAreaComponent } from '@static/components/inline-text-area/inline-text-area.component';
import { TaskDates } from '@static/components/task-dates/task-dates.component';
import { UserSelectComponent } from '@static/components/user-select/user-select.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { TaskStatusPipe } from '@static/pipes/task-status.pipe';
import { Subject } from 'rxjs';
import {
  debounceTime,
  first,
  takeUntil,
  tap,
  withLatestFrom,
} from 'rxjs/operators';

@Component({
  selector: 'app-task-detail-dialog',
  templateUrl: './task-detail-dialog.component.html',
  styleUrls: ['./task-detail-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    MatIcon,
    ActivityMenuComponent,
    InlineTextAreaComponent,
    UserSelectComponent,
    AvatarComponent,
    MatChipListbox,
    MatChipOption,
    MatMenuTrigger,
    MatMenu,
    MatMenuItem,
    MatIconButton,
    MatTooltip,
    AutocompleteChipsComponent,
    CommentsListComponent,
    EditorComponent,
    DialogActionsDirective,
    MatProgressSpinner,
    TaskStatusPipe,
    TaskDates,
  ],
})
export class TaskDetailDialogComponent implements OnDestroy, AfterViewInit {
  dialogRef = inject<DialogRef<TaskDetailDialogComponent>>(DialogRef);
  data = inject<TaskViewModel>(DIALOG_DATA, { optional: false });
  private store = inject(Store);
  private actions$ = inject<Actions<Action>>(Actions);

  static width = '972px';

  projects = this.store.selectSignal(ProjectSelectors.selectAllProjects);
  comments = this.store.selectSignal(TaskSelectors.selectComments);
  tags = this.store.selectSignal(TagsSelectors.selectTagNames);
  user = this.store.selectSignal(selectRequiredCurrentUser);
  users = this.store.selectSignal(UsersSelectors.selectAllUsers);

  editorLoaded = signal(false);

  task = this.store.selectSignal(TaskSelectors.selectDetailTask);

  selectedTypeValue!: number;
  entityType = EntityType.task;

  onDestroy$ = new Subject<void>();

  formGroup!: FormGroup;

  get name() {
    return this.formGroup.controls.name;
  }

  get description() {
    return this.formGroup.controls.description;
  }

  constructor() {
    effect(() => {
      const task = this.task();

      if (!task) return;

      this.buildForm(task);
      this.loadComments(task);
    });
  }

  ngAfterViewInit() {
    const systemId: string = this.data?.systemId;

    this.store.dispatch(TaskActions.loadTaskDetails({ systemId }));
  }

  buildForm(task: TaskViewModel) {
    this.formGroup = new FormGroup({
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
          const updated: UpdateProjectTaskRequest = {
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
          const updated: UpdateProjectTaskRequest = {
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
          if (!viewModel) return;

          const request: AddCommentRequest = {
            comment: value.trim(),
            systemId: viewModel.systemId,
          };

          this.store.dispatch(TaskActions.addComment({ request }));
        })
      )
      .subscribe();
  }

  updateTask(task: UpdateProjectTaskRequest) {
    this.store
      .select(selectCurrentHubGroupId)
      .pipe(
        first(),
        tap((identifier) => {
          if (!identifier) return;

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
          if (!task) return;

          const updated: UpdateProjectTaskRequest = {
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
          if (!task) return;

          const updated: UpdateProjectTaskRequest = {
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
          if (!task) return;

          const assigneeSet = new Set(task.assignees.map((u) => u.id));

          if (assigneeSet.has(user.id)) {
            assigneeSet.delete(user.id);
          } else {
            assigneeSet.add(user.id);
          }

          const assigneeIds = Array.from(assigneeSet);
          const updated: UpdateProjectTaskRequest = {
            ...task,
            assigneeIds,
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
          if (!task || !identifier) return;

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
          if (!task || !identifier) return;

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
    this.editorLoaded.set(true);
  }
}
