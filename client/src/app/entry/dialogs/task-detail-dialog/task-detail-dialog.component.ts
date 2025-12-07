import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  OnDestroy,
  signal,
} from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import {
  debounce,
  Field,
  form,
  minLength,
  required,
} from '@angular/forms/signals';
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
import { Store } from '@ngrx/store';
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
    Field,
  ],
})
export class TaskDetailDialogComponent implements OnDestroy {
  dialogRef = inject<DialogRef<TaskDetailDialogComponent>>(DialogRef);
  data = inject<TaskViewModel>(DIALOG_DATA, { optional: false });
  private store = inject(Store);

  static width = '972px';

  projects = this.store.selectSignal(ProjectSelectors.selectAllProjects);
  comments = this.store.selectSignal(TaskSelectors.selectComments);
  tags = this.store.selectSignal(TagsSelectors.selectTagNames);
  user = this.store.selectSignal(selectRequiredCurrentUser);
  users = this.store.selectSignal(UsersSelectors.selectAllUsers);

  selectedTypeValue!: number;
  entityType = EntityType.task;

  editorLoaded = signal(false);

  task = this.store.selectSignal(TaskSelectors.selectDetailTask);
  hubGroupId = this.store.selectSignal(selectCurrentHubGroupId);

  taskFormModel = signal({
    name: '',
    description: '',
  });

  taskForm = form(this.taskFormModel, (schema) => {
    required(schema.name);
    minLength(schema.name, 4);
    debounce(schema.name, 300);
    debounce(schema.description, 300);
  });

  constructor() {
    effect(() => {
      const task = this.task();
      const touched = this.taskForm().touched();

      if (!task || touched) return;

      this.taskFormModel.set({
        name: task.name,
        description: task.description,
      });
    });

    effect(() => {
      const touched = this.taskForm().touched();
      const valid = this.taskForm().valid();

      if (!touched || !valid) return;

      const updated = {
        name: this.taskForm.name().value(),
        description: this.taskForm.description().value(),
      };

      this.updateTask(updated);
    });

    const systemId: string = this.data?.systemId;

    if (systemId) {
      this.store.dispatch(TaskActions.loadTaskDetails({ systemId }));
    }
  }

  onCommentSubmit(value: string) {
    if (!value) return;

    const task = this.task();

    if (!task) return;

    const request: AddCommentRequest = {
      comment: value.trim(),
      systemId: task.systemId,
    };

    this.store.dispatch(TaskActions.addComment({ request }));
  }

  updateTask(update: Partial<UpdateProjectTaskRequest>) {
    this.taskForm().reset();

    const identifier = this.hubGroupId();
    const task = this.task();

    if (!identifier || !task) return;

    this.store.dispatch(
      TaskActions.editProjectTask({
        identifier,
        task: {
          ...task,
          ...update,
        },
      })
    );
  }

  close() {
    this.dialogRef.close();
  }

  ngOnDestroy() {
    this.store.dispatch(TaskActions.clearTaskDetail());
  }

  onFlagClicked() {
    const task = this.task();

    if (!task) return;

    const updated: UpdateProjectTaskRequest = {
      ...task,
      isFlagged: !task.isFlagged,
    };

    this.updateTask(updated);
  }

  selectProject(projectId: number) {
    const task = this.task();

    if (!task) return;

    const updated: UpdateProjectTaskRequest = {
      ...task,
      projectId,
    };

    this.updateTask(updated);
  }

  selectAssignee(user: AppUser) {
    const task = this.task();

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
  }

  deleteClicked() {
    const task = this.task();
    const identifier = this.hubGroupId();

    if (!task || !identifier) return;

    this.store.dispatch(TaskActions.deleteProjectTask({ identifier, task }));
    this.dialogRef.close();
  }

  onDeleteCommentClicked(comment: CommentViewModel) {
    const commentId = comment.id;
    this.store.dispatch(TaskActions.deleteComment({ commentId }));
  }

  onTagsSelectionChanged(event: AutocompleteChipsSelectionChanged) {
    const task = this.task();
    const identifier = this.hubGroupId();

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

      this.store.dispatch(TaskActions.addTagToTask({ identifier, request }));
    }
  }

  onEditorLoaded() {
    this.editorLoaded.set(true);
  }
}
