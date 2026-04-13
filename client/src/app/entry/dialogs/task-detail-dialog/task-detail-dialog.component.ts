import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  OnDestroy,
  signal,
} from '@angular/core';
import {
  debounce,
  form,
  FormField,
  minLength,
  required,
} from '@angular/forms/signals';
import { netptunePermissions } from '@app/core/auth/permissions';
import { FormSelectTagsOptionComponent } from '@app/static/components/form-select-tags/form-select-tags-option.component';
import { FormSelectTagsComponent } from '@app/static/components/form-select-tags/form-select-tags.component';
import { SpinnerComponent } from '@app/static/components/spinner/spinner.component';
import {
  selectCurrentUser,
  selectHasPermission,
} from '@core/auth/store/auth.selectors';
import { CommentViewModel } from '@core/models/comment';
import { EntityType } from '@core/models/entity-type';
import { AddCommentRequest } from '@core/models/requests/add-comment-request';
import { UpdateProjectTaskRequest } from '@core/models/requests/update-project-task-request';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { selectCurrentHubGroupId } from '@core/store/hub-context/hub-context.selectors';
import * as ProjectSelectors from '@core/store/projects/projects.selectors';
import * as TagsSelectors from '@core/store/tags/tags.selectors';
import * as TaskActions from '@core/store/tasks/tasks.actions';
import * as TaskSelectors from '@core/store/tasks/tasks.selectors';
import * as UsersSelectors from '@core/store/users/users.selectors';
import { ActivityMenuComponent } from '@entry/components/activity-menu/activity-menu.component';
import { LucideCheck } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { CommentsListComponent } from '@static/components/comments-list/comments-list.component';
import { EditorComponent } from '@static/components/editor/editor.component';
import { InlineTextAreaComponent } from '@static/components/inline-text-area/inline-text-area.component';
import { TaskDates } from '@static/components/task-dates/task-dates.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { TaskDetailPropertiesComponent } from './task-detail-properties.component';

@Component({
  selector: 'app-task-detail-dialog',
  template: `
    @if (task(); as task) {
      <div>
        <form (submit)="onSubmit($event)">
          <div class="mb-1 flex flex-row items-center justify-end gap-4">
            @if (task.status === 1) {
              <svg lucideCheck class="h-4 w-4 text-green-500"></svg>
            }
            <app-task-scope-id [id]="task.systemId" />
            <app-activity-menu [entityType]="entityType" [entityId]="task.id" />
          </div>
          <div class="px-12 py-0">
            <app-inline-text-area
              class="form-control"
              [formField]="taskForm.name"
              activeBorder="true"
              [isReadonly]="isReadOnly()" />
          </div>
          <div class="px-12 py-0">
            <div class="flex flex-col">
              <app-task-detail-properties />
              <div>
                <h4 class="font-sm mt-4 mb-2 font-semibold">Tags</h4>
                <app-form-select-tags
                  class="tags-autocomplete"
                  placeholder="Add a Tag..."
                  [value]="selectedTags()"
                  (changed)="onTagsSelectionChanged($event)"
                  [isReadonly]="isReadOnly()">
                  @for (tag of tags(); track tag) {
                    <app-form-select-tags-option [value]="tag">
                      {{ tag }}
                    </app-form-select-tags-option>
                  }
                </app-form-select-tags>
              </div>
              <div>
                <h4 class="font-sm mt-4 mb-2 font-semibold">Comments</h4>
                @if (editorLoaded()) {
                  <app-comments-list
                    [user]="user()"
                    [comments]="comments()"
                    (commentSubmit)="onCommentSubmit($event)"
                    (deleteComment)="onDeleteCommentClicked($event)"
                    [canDelete]="canDeleteComment()"
                    [canCreate]="canCreateComment()">
                  </app-comments-list>
                }
              </div>
            </div>
            <label class="font-sm font-semibold" for="description"
              >Description</label
            >
            <app-editor
              class="border-foreground/30 mt-2 flex max-h-[calc(100vh-960px)] overflow-y-auto rounded-sm border-2 px-4 py-1"
              aria-labelledby="description"
              [formField]="taskForm.description"
              placeholder="Add a Description..."
              (loaded)="onEditorLoaded()"
              [isReadOnly]="isReadOnly()">
            </app-editor>
          </div>
        </form>
      </div>
      <div app-dialog-actions align="end">
        <app-task-dates [task]="task" />
      </div>
    } @else {
      <div class="flex h-243.5 flex-col items-center justify-center">
        <app-spinner diameter="64" />
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    LucideCheck,
    ActivityMenuComponent,
    InlineTextAreaComponent,
    FormSelectTagsComponent,
    CommentsListComponent,
    EditorComponent,
    DialogActionsDirective,
    SpinnerComponent,
    TaskDates,
    FormField,
    TaskScopeIdComponent,
    FormSelectTagsOptionComponent,
    TaskDetailPropertiesComponent,
  ],
})
export class TaskDetailDialogComponent implements OnDestroy {
  dialogRef = inject<DialogRef<TaskDetailDialogComponent>>(DialogRef);
  data = inject<TaskViewModel>(DIALOG_DATA, { optional: false });
  store = inject(Store);

  public static width = '972px';

  projects = this.store.selectSignal(ProjectSelectors.selectAllProjects);
  comments = this.store.selectSignal(TaskSelectors.selectComments);
  tags = this.store.selectSignal(TagsSelectors.selectTagNames);
  user = this.store.selectSignal(selectCurrentUser);
  users = this.store.selectSignal(UsersSelectors.selectAllUsers);

  selectedTypeValue!: number;
  entityType = EntityType.task;

  editorLoaded = signal(false);

  task = this.store.selectSignal(TaskSelectors.selectDetailTask);
  hubGroupId = this.store.selectSignal(selectCurrentHubGroupId);
  selectedTags = computed(() => this.task()?.tags ?? []);

  canCreateComment = this.store.selectSignal(
    selectHasPermission(netptunePermissions.comments.create)
  );

  canDeleteComment = this.store.selectSignal(
    selectHasPermission(netptunePermissions.comments.deleteOwn)
  );

  canUpdateTask = this.store.selectSignal(
    selectHasPermission(netptunePermissions.tasks.update)
  );

  isReadOnly = computed(() => !this.canUpdateTask());

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

  onSubmit(event: Event) {
    event.preventDefault();
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

  onDeleteCommentClicked(comment: CommentViewModel) {
    const commentId = comment.id;
    this.store.dispatch(TaskActions.deleteComment({ commentId }));
  }

  onTagsSelectionChanged(tags: string[]) {
    const task = this.task();
    const identifier = this.hubGroupId();

    if (!task || !identifier) return;

    const currentTags = new Set(task.tags);
    const nextTags = new Set(tags);

    const added = tags.find((t) => !currentTags.has(t));
    const removed = task.tags.find((t) => !nextTags.has(t));

    if (removed) {
      this.store.dispatch(
        TaskActions.deleteTagFromTask({
          identifier,
          systemId: task.systemId,
          tag: removed,
        })
      );
    } else if (added) {
      this.store.dispatch(
        TaskActions.addTagToTask({
          identifier,
          request: {
            systemId: task.systemId,
            tag: added,
          },
        })
      );
    }
  }

  onEditorLoaded() {
    this.editorLoaded.set(true);
  }
}
