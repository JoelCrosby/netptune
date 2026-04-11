import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
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
import { LucideCheck, LucideFlag, LucideTrash2 } from '@lucide/angular';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';
import { FormSelectTagsOptionComponent } from '@app/static/components/form-select-tags/form-select-tags-option.component';
import { FormSelectTagsComponent } from '@app/static/components/form-select-tags/form-select-tags.component';
import { SpinnerComponent } from '@app/static/components/spinner/spinner.component';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { selectRequiredCurrentUser } from '@core/auth/store/auth.selectors';
import { AppUser } from '@core/models/appuser';
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
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { ChipListboxComponent } from '@static/components/chip/chip-listbox.component';
import { ChipOptionComponent } from '@static/components/chip/chip-option.component';
import { CommentsListComponent } from '@static/components/comments-list/comments-list.component';
import { EditorComponent } from '@static/components/editor/editor.component';
import { InlineTextAreaComponent } from '@static/components/inline-text-area/inline-text-area.component';
import { TaskDates } from '@static/components/task-dates/task-dates.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { UserSelectComponent } from '@app/static/components/user-select/user-select.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { TaskStatusPipe } from '@static/pipes/task-status.pipe';

@Component({
  selector: 'app-task-detail-dialog',
  template: `
    @if (task(); as task) {
      <div>
        <form (submit)="onSubmit($event)">
          <div class="flex flex-row justify-end gap-4 items-center mb-1">
            @if (task.status === 1) {
              <svg lucideCheck class="h-4 w-4 text-green-500"></svg>
            }
            <app-task-scope-id [id]="task.systemId" />
            <app-activity-menu [entityType]="entityType" [entityId]="task.id" />
          </div>
          <div class="px-12 py-[0]">
            <app-inline-text-area
              class="form-control"
              [formField]="taskForm.name"
              activeBorder="true"
            />
          </div>
          <div class="px-12 py-[0]">
            <div class="flex flex-col">
              <div>
                <h4 class="font-sm font-semibold mb-2 mt-4">Assignees</h4>
                <app-user-select
                  [value]="task.assignees"
                  [options]="users()"
                  (selectChange)="selectAssignee($event)"
                />
              </div>
              <div>
                <h4 class="font-sm font-semibold mb-2 mt-4">Reporter</h4>
                <div class="flex flex-row items-center rounded">
                  <app-avatar
                    size="24"
                    [name]="task.ownerUsername"
                    [imageUrl]="task.ownerPictureUrl"
                  >
                  </app-avatar>
                  <small class="font-medium text-sm ml-2">{{ task.ownerUsername }}</small>
                </div>
              </div>
              <div>
                <h4 class="font-sm font-semibold mb-2 mt-4">Status</h4>
                <app-chip-listbox>
                  <app-chip-option>{{ task.status | taskStatus }}</app-chip-option>
                </app-chip-listbox>
              </div>
              <div>
                <h4 class="font-sm font-semibold mb-2 mt-4">Project</h4>
                <app-chip-listbox>
                  <button app-chip-option (click)="projectsMenu.toggle($any($event.currentTarget))">
                    {{ task.projectName }}
                  </button>
                </app-chip-listbox>
                <app-dropdown-menu #projectsMenu>
                  <small class="block px-3 py-1 text-xs text-neutral-500">Change Project</small>
                  @for (project of projects(); track project.id) {
                    <button app-menu-item (click)="selectProject(project.id); projectsMenu.close()">
                      {{ project.name }}
                    </button>
                  }
                </app-dropdown-menu>
              </div>
            </div>
            <div>
              <h4 class="font-sm font-semibold mb-2 mt-4">Actions</h4>
              <div class="flex gap-2">
                <button
                  app-stroked-button
                  aria-label="Delete Task"
                  appTooltip="Delete Task"
                  (click)="deleteClicked()"
                >
                  <svg lucideTrash2 class="h-4 w-4"></svg>
                </button>
                <button
                  app-stroked-button
                  aria-label="Flag Task"
                  appTooltip="Flag Task"
                  color="warn"
                  (click)="onFlagClicked()"
                >
                  <svg lucideFlag class="h-4 w-4" [class.fill-current]="task.isFlagged"></svg>
                </button>
              </div>
            </div>
            <div class="flex flex-col">
              <div>
                <h4 class="font-sm font-semibold mb-2 mt-4">Tags</h4>
                <app-form-select-tags
                  class="tags-autocomplete"
                  placeholder="Add a Tag..."
                  (changed)="onTagsSelectionChanged($event)"
                >
                  @for (tag of tags(); track tag) {
                    <app-form-select-tags-option [value]="tag">
                      {{ tag }}
                    </app-form-select-tags-option>
                  }
                </app-form-select-tags>
              </div>
              <div>
                <h4 class="font-sm font-semibold mb-2 mt-4">Comments</h4>
                @if (editorLoaded()) {
                  <app-comments-list
                    [user]="user()"
                    [comments]="comments()"
                    (commentSubmit)="onCommentSubmit($event)"
                    (deleteComment)="onDeleteCommentClicked($event)"
                  >
                  </app-comments-list>
                }
              </div>
            </div>
            <label class="font-sm font-semibold" for="description">Description</label>
            <app-editor
              class="border-2 border border-neutral-700 rounded-sm flex overflow-y-auto max-h-[calc(100vh_-_960px)] px-4 py-1 mt-2"
              aria-labelledby="description"
              [formField]="taskForm.description"
              placeholder="Add a Description..."
              (loaded)="onEditorLoaded()"
            >
            </app-editor>
          </div>
        </form>
      </div>
      <div app-dialog-actions align="end">
        <app-task-dates [task]="task" />
      </div>
    } @else {
      <div class="h-[974px] flex flex-col justify-center items-center">
        <app-spinner diameter="64" />
      </div>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    LucideCheck,
    LucideFlag,
    LucideTrash2,
    ActivityMenuComponent,
    InlineTextAreaComponent,
    UserSelectComponent,
    AvatarComponent,
    ChipListboxComponent,
    ChipOptionComponent,
    DropdownMenuComponent,
    MenuItemComponent,
    StrokedButtonComponent,
    TooltipDirective,
    FormSelectTagsComponent,
    CommentsListComponent,
    EditorComponent,
    DialogActionsDirective,
    SpinnerComponent,
    TaskStatusPipe,
    TaskDates,
    FormField,
    TaskScopeIdComponent,
    FormSelectTagsOptionComponent,
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
