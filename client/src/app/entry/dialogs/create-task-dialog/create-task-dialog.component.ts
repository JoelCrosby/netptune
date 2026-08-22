import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { httpResource } from '@angular/common/http';
import {
  Component,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import {
  FormField,
  form,
  maxLength,
  required,
  submit,
  validate,
} from '@angular/forms/signals';
import { EditorComponent } from '@app/static/components/editor/editor.component';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { EstimateType } from '@core/enums/estimate-type';
import { TaskPriority } from '@core/enums/task-priority';
import { MAX_PAGE_SIZE } from '@core/models/pagination';
import { AddProjectTaskRequest } from '@core/models/project-task';
import { RelationType } from '@core/models/relation-type';
import { Tag } from '@core/models/tag';
import { AddTaskRelationRequest } from '@core/models/task-relation';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { UserSelectValue } from '@core/models/view-models/user-select-option';
import { WorkspaceFileContentTypeGroup } from '@core/models/view-models/workspace-file-view-model';
import { CurrentProjectService } from '@core/services/current-project.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { DialogService } from '@core/services/dialog.service';
import { SessionService } from '@core/services/session.service';
import { TaskCommandsService } from '@core/services/task-commands.service';
import { TaskFileUploadService } from '@core/services/task-file-upload.service';
import { colorSwatchClass } from '@core/util/colors/colors';
import { reloadOnRefresh } from '@core/util/reload-on-refresh';
import { LucideLink2, LucidePlus, LucideX } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FileDropzoneComponent } from '@static/components/file-dropzone/file-dropzone.component';
import { FileTypeIconComponent } from '@static/components/file-type-icon/file-type-icon.component';
import { FormErrorsComponent } from '@static/components/form-error/form-errors.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectTagsOptionComponent } from '@static/components/form-select-tags/form-select-tags-option.component';
import { FormSelectTagsComponent } from '@static/components/form-select-tags/form-select-tags.component';
import { TaskEstimate } from '@static/components/task-properties/task-estimate-select.component';
import {
  TaskPropertiesComponent,
  TaskReporter,
} from '@static/components/task-properties/task-properties.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { FileSizePipe } from '@static/pipes/file-size.pipe';
import {
  LinkTaskDialogComponent,
  LinkTaskDialogData,
  LinkTaskDialogResult,
} from '../link-task-dialog/link-task-dialog.component';

export interface CreateTaskDialogData {
  projectId?: number;
  sprintId?: number;
}

interface CreateTaskForm {
  name: string;
  description: string;
}

interface StagedRelation {
  relationTypeId: number;
  label: string;
  taskIsSource: boolean;
  task: TaskViewModel;
}

interface StagedRelationGroup {
  label: string;
  relations: StagedRelation[];
}

const archiveContentTypes = new Set([
  'application/zip',
  'application/x-zip-compressed',
  'application/x-tar',
  'application/gzip',
  'application/x-7z-compressed',
  'application/vnd.rar',
]);

const documentContentTypes = new Set([
  'application/pdf',
  'application/msword',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  'application/vnd.ms-excel',
  'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  'application/json',
]);

@Component({
  imports: [
    DialogTitleComponent,
    FormField,
    FormInputComponent,
    DialogActionsDirective,
    FileDropzoneComponent,
    FileSizePipe,
    FileTypeIconComponent,
    FlatButtonComponent,
    IconButtonComponent,
    StrokedButtonComponent,
    EditorComponent,
    FormErrorsComponent,
    FormSelectTagsComponent,
    FormSelectTagsOptionComponent,
    LucideLink2,
    LucidePlus,
    LucideX,
    TaskPropertiesComponent,
    TaskScopeIdComponent,
    TooltipDirective,
  ],
  providers: [TaskFileUploadService],
  template: `
    <app-dialog-title>
      <div class="px-6">
        <span i18n="Title of the create-task dialog">Create Task</span>
      </div>
    </app-dialog-title>

    <form
      id="create-task-form"
      app-dialog-content
      novalidate
      (submit)="saveClicked($event)">
      <div class="flex flex-col gap-8 px-6 md:flex-row md:gap-12">
        <div class="flex w-92 grow flex-col">
          <app-form-input
            [formField]="taskForm.name"
            i18n-label="Label of the task title field"
            label="Summary"
            maxLength="256" />

          @if (canAssignTags()) {
            <h4 class="font-sm mt-4 mb-2 font-semibold">
              <span i18n="Section heading for a task's tags">Tags</span>
            </h4>
            <app-form-select-tags
              class="tags-autocomplete"
              i18n-placeholder="
                Placeholder in the box for adding a tag to a task
              "
              placeholder="Add a Tag..."
              [value]="selectedTags()"
              [isReadonly]="busy()"
              (changed)="selectedTags.set($event)">
              @for (tag of tagNames(); track tag) {
                <app-form-select-tags-option [value]="tag">
                  {{ tag }}
                </app-form-select-tags-option>
              }
            </app-form-select-tags>
          }

          <label
            id="description-label"
            class="font-sm mt-4 mb-2 font-semibold"
            for="description">
            <span i18n="Label of the task description editor">Description</span>
          </label>
          <app-editor
            id="description"
            aria-labelledby="description-label"
            i18n-placeholder="Placeholder in the empty task description editor"
            placeholder="Add a Description..."
            [formField]="taskForm.description"
            [isReadonly]="false" />
          <app-form-errors [formField]="taskForm.description" />

          @if (canUploadFiles()) {
            <section class="mt-4" aria-labelledby="create-task-files-heading">
              <h4
                id="create-task-files-heading"
                class="font-sm mb-2 font-semibold">
                <span i18n="Heading for a task's files">Files</span>
              </h4>

              <app-file-dropzone
                [disabled]="busy()"
                [maxBytes]="maxUploadBytes()"
                (filesSelected)="addFiles($event)" />

              <div class="mt-2 flex flex-col gap-1">
                @for (file of stagedFiles(); track file.name + file.size) {
                  <div
                    class="border-border flex items-center gap-3 rounded border p-2">
                    <app-file-type-icon
                      size="small"
                      [group]="fileGroup(file)" />
                    <div class="min-w-0 flex-1">
                      <span class="block truncate font-medium">
                        {{ file.name }}
                      </span>
                      <span class="text-muted text-xs">
                        {{ file.size | fileSize }}
                      </span>
                    </div>
                    <button
                      app-icon-button
                      type="button"
                      [disabled]="busy()"
                      i18n-aria-label="
                        Accessible label for the button that takes a file off a
                        task that has not been created yet
                      "
                      aria-label="Remove file"
                      (click)="removeFile(file)">
                      <svg lucideX class="h-4 w-4"></svg>
                    </button>
                  </div>
                }
              </div>

              <div class="mt-2 flex flex-col gap-2" aria-live="polite">
                @for (upload of uploads(); track upload.id) {
                  <div class="bg-card rounded p-2 text-sm">
                    <div class="flex items-center justify-between gap-2">
                      <span class="truncate">{{ upload.name }}</span>
                      @if (upload.error) {
                        <span class="text-destructive ml-auto">
                          {{ upload.error }}
                        </span>
                      } @else {
                        <span>{{ upload.progress }}%</span>
                      }
                    </div>
                    <div class="bg-muted mt-1 h-1 overflow-hidden rounded">
                      <div
                        class="bg-primary h-full"
                        [style.width.%]="upload.progress"></div>
                    </div>
                  </div>
                }
              </div>

              @if (uploadsFailed()) {
                <p class="text-destructive mt-2 text-sm" role="alert">
                  <span
                    i18n="
                      Shown when a new task was saved but some of its files did
                      not upload
                    ">
                    The task was created, but some files did not upload. Close
                    this dialog and add them from the task.
                  </span>
                </p>
              }
            </section>
          }

          @if (canLinkTasks()) {
            <section
              class="mt-4"
              aria-labelledby="create-task-relations-heading">
              <div class="mb-2 flex items-center justify-between">
                <h4
                  id="create-task-relations-heading"
                  class="font-sm font-semibold">
                  <span i18n="Section heading for links between tasks">
                    Relations
                  </span>
                </h4>
                <button
                  app-stroked-button
                  type="button"
                  size="sm"
                  [disabled]="busy()"
                  (click)="openLinkDialog()">
                  <svg lucidePlus class="h-4 w-4"></svg>
                  <span i18n="Links a task to another">Link task</span>
                </button>
              </div>

              @for (group of relationGroups(); track group.label) {
                <div class="mb-3">
                  <div
                    class="text-muted mb-1 text-xs font-medium tracking-wide uppercase">
                    {{ group.label }}
                  </div>

                  <ul class="flex flex-col gap-1">
                    @for (relation of group.relations; track relation.task.id) {
                      <li
                        class="border-border bg-card flex items-center gap-3 rounded border px-3 py-2">
                        <span
                          [class]="
                            'h-2 w-2 shrink-0 rounded-full ' +
                            colorSwatchClass(relation.task.statusColor)
                          "></span>

                        <app-task-scope-id [id]="relation.task.systemId" />

                        <span class="flex-1 truncate">
                          {{ relation.task.name }}
                        </span>

                        <button
                          app-icon-button
                          type="button"
                          [disabled]="busy()"
                          i18n-appTooltip="
                            Tooltip on the button that removes a task link
                          "
                          appTooltip="Remove link"
                          i18n-aria-label="
                            Accessible label for the button that removes a task
                            link
                          "
                          aria-label="Remove link"
                          (click)="removeRelation(relation)">
                          <svg lucideX class="h-4 w-4"></svg>
                        </button>
                      </li>
                    }
                  </ul>
                </div>
              } @empty {
                <div class="text-muted flex items-center gap-2 text-sm">
                  <svg lucideLink2 class="h-4 w-4"></svg>
                  <span
                    i18n="Empty state when a task has no links to other tasks">
                    No linked tasks
                  </span>
                </div>
              }
            </section>
          }
        </div>

        <div
          class="bg-card/40 flex w-full shrink-0 flex-col rounded px-6 pb-6 md:w-72">
          <app-task-properties
            [(statusId)]="statusId"
            [(priority)]="priority"
            [(projectId)]="projectId"
            [(sprintId)]="sprintId"
            [(startDate)]="startDate"
            [(dueDate)]="dueDate"
            [(assignees)]="assignees"
            [estimateType]="estimateType()"
            [estimateValue]="estimateValue()"
            [reporter]="reporter()"
            [showProject]="!data?.projectId"
            [showSprint]="!data?.sprintId"
            [editable]="!busy()"
            (estimateChange)="setEstimate($event)" />

          @if (scheduleInvalid()) {
            <p class="mt-3 text-sm text-red-600" role="alert">
              <span
                i18n="
                  Validation error when a task's start date is after its due
                  date
                ">
                Start date must be on or before due date.
              </span>
            </p>
          }

          @if (projectInvalid()) {
            <p class="mt-3 text-sm text-red-600" role="alert">
              <span
                i18n="Validation error when no project is selected for a task">
                Project is required.
              </span>
            </p>
          }
        </div>
      </div>
    </form>

    <div app-dialog-actions align="end">
      <button app-stroked-button type="button" (click)="close()">
        <span i18n="Dismisses a dialog without saving">Close</span>
      </button>
      <button
        app-flat-button
        type="submit"
        form="create-task-form"
        [disabled]="busy()">
        <span i18n="Button that saves the new task">Save Task</span>
      </button>
    </div>
  `,
})
export class CreateTaskDialogComponent {
  static readonly width = '972px';

  protected readonly colorSwatchClass = colorSwatchClass;

  private taskCommands = inject(TaskCommandsService);
  private dialog = inject(DialogService);
  private session = inject(SessionService);
  private uploadService = inject(TaskFileUploadService);
  dialogRef = inject<DialogRef<CreateTaskDialogComponent>>(DialogRef);
  readonly data = inject<CreateTaskDialogData | null>(DIALOG_DATA, {
    optional: true,
  });

  currentProjectId = inject(CurrentProjectService).currentId;
  readonly maxUploadBytes = inject(CurrentWorkspaceService).maxUploadBytes;

  private readonly canReadTags = hasPermission(PERMISSIONS.tags.read);
  private readonly canAssignTagsToTasks = hasPermission(
    PERMISSIONS.tags.assign
  );
  readonly canUploadFiles = hasPermission(PERMISSIONS.files.upload);
  readonly canLinkTasks = hasPermission(PERMISSIONS.tasks.update);

  // Picking tags means both listing the workspace's tags and being allowed to attach one.
  readonly canAssignTags = computed(() => {
    return this.canReadTags() && this.canAssignTagsToTasks();
  });

  readonly tags = httpResource<Tag[]>(
    () => {
      if (!this.canAssignTags()) return undefined;

      return {
        url: 'api/tags/workspace',
        params: {
          page: 1,
          pageSize: MAX_PAGE_SIZE,
        },
      };
    },
    { defaultValue: [] }
  );

  // Whoever opens the dialog becomes the task's reporter, so it reads the same as the detail dialog.
  readonly reporter = computed<TaskReporter | null>(() => {
    const user = this.session.currentUser();

    if (!user) return null;

    return {
      displayName: user.displayName || user.email,
      pictureUrl: user.pictureUrl,
    };
  });

  readonly tagNames = computed(() => this.tags.value().map((tag) => tag.name));
  readonly selectedTags = signal<string[]>([]);
  readonly stagedFiles = signal<File[]>([]);
  readonly stagedRelations = signal<StagedRelation[]>([]);
  readonly uploads = this.uploadService.uploads;

  // Set once the task exists, which is also the point the dialog stops accepting edits.
  private readonly createdSystemId = signal<string | null>(null);
  private readonly saving = signal(false);

  readonly created = computed(() => this.createdSystemId() !== null);
  readonly busy = computed(() => this.saving() || this.created());
  readonly uploadsFailed = computed(() => {
    return this.uploads().some((upload) => Boolean(upload.error));
  });

  readonly statusId = signal<number | null>(null);
  readonly priority = signal<TaskPriority | null>(null);
  readonly estimateType = signal<EstimateType | null>(null);
  readonly estimateValue = signal<number | null>(null);
  readonly sprintId = signal<number | null>(this.data?.sprintId ?? null);
  readonly startDate = signal('');
  readonly dueDate = signal('');
  readonly projectId = signal<number | null>(
    this.data?.projectId ?? this.currentProjectId() ?? null
  );
  readonly assignees = signal<UserSelectValue[]>([]);
  readonly submissionAttempted = signal(false);
  readonly scheduleInvalid = computed(() => {
    const startDate = this.startDate();
    const dueDate = this.dueDate();

    return startDate !== '' && dueDate !== '' && startDate > dueDate;
  });
  readonly projectInvalid = computed(
    () => this.submissionAttempted() && this.projectId() === null
  );

  // Relations arrive one link dialog at a time, so they are grouped for display the same way the
  // task detail dialog groups them: by the label the link reads as in this direction.
  readonly relationGroups = computed<StagedRelationGroup[]>(() => {
    const groups: StagedRelationGroup[] = [];

    for (const relation of this.stagedRelations()) {
      const existing = groups.find((group) => group.label === relation.label);

      if (existing) {
        existing.relations.push(relation);
        continue;
      }

      groups.push({ label: relation.label, relations: [relation] });
    }

    return groups;
  });

  taskFormModel = signal<CreateTaskForm>({
    name: '',
    description: '',
  });

  taskForm = form(this.taskFormModel, (schema) => {
    required(schema.name, {
      message: $localize`:Body of a dialog or validation message:Summary is required.`,
    });
    validate(schema.name, ({ value }) => {
      const valueToValidate = value();

      if (!valueToValidate) return undefined;

      const name = valueToValidate.trim();

      if (!name) {
        return {
          kind: 'whitespace',
          message: $localize`:Body of a dialog or validation message:Summary is required.`,
        };
      }

      if (name.length < 4) {
        return {
          kind: 'minLength',
          message: $localize`:Body of a dialog or validation message:Summary must have at least 4 characters.`,
        };
      }

      if (name.length > 256) {
        return {
          kind: 'maxLength',
          message: $localize`:Body of a dialog or validation message:Summary cannot exceed 256 characters.`,
        };
      }

      return undefined;
    });
    maxLength(schema.description, 4096, {
      message: $localize`:Body of a dialog or validation message:Description cannot exceed 4096 characters.`,
    });
  });

  constructor() {
    reloadOnRefresh(this.tags, ['tags']);

    // Files can only be attached once the task has an id, so the dialog stays open through the
    // uploads and closes itself when they land. Failures keep it open with the reason showing.
    effect(() => {
      if (!this.created()) return;

      const uploads = this.uploads();
      const hasPendingUploads = this.uploadService.uploading();
      const isSettled = uploads.length > 0 && !hasPendingUploads;

      if (!isSettled || this.uploadsFailed()) return;

      untracked(() => this.dialogRef.close());
    });
  }

  setEstimate({ estimateType, estimateValue }: TaskEstimate) {
    this.estimateType.set(estimateType);
    this.estimateValue.set(estimateValue);
  }

  // Staged files have not been through the server's own grouping yet, so the icon is picked from
  // the browser's content type.
  fileGroup(file: File): WorkspaceFileContentTypeGroup {
    if (file.type.startsWith('image/')) return 'image';

    if (archiveContentTypes.has(file.type)) return 'archive';

    const isDocument =
      file.type.startsWith('text/') || documentContentTypes.has(file.type);

    return isDocument ? 'document' : 'other';
  }

  addFiles(files: File[]) {
    this.stagedFiles.update((staged) => {
      const names = new Set(staged.map((file) => `${file.name}:${file.size}`));
      const added = files.filter(
        (file) => !names.has(`${file.name}:${file.size}`)
      );

      return [...staged, ...added];
    });
  }

  removeFile(file: File) {
    this.stagedFiles.update((staged) => staged.filter((item) => item !== file));
  }

  openLinkDialog() {
    const dialogRef = this.dialog.open<
      LinkTaskDialogResult,
      LinkTaskDialogData
    >(LinkTaskDialogComponent, { data: {}, width: '900px' });

    dialogRef.closed.subscribe((result) => {
      if (!result) return;

      this.stageRelations(result);
    });
  }

  removeRelation(relation: StagedRelation) {
    this.stagedRelations.update((staged) =>
      staged.filter((item) => item !== relation)
    );
  }

  close() {
    this.dialogRef.close();
  }

  saveClicked(event: Event) {
    event.preventDefault();
    this.submissionAttempted.set(true);

    submit(this.taskForm, async () => {
      if (this.scheduleInvalid() || this.busy()) return;

      const projectId = this.projectId();

      if (projectId === null) return;

      this.saving.set(true);

      this.taskCommands.create(this.buildRequest(projectId), {
        onCreated: (created) => {
          return this.onCreated(created);
        },
        onFailed: () => {
          return this.saving.set(false);
        },
      });
    });
  }

  private onCreated(created: TaskViewModel) {
    const files = this.stagedFiles();

    this.saving.set(false);

    if (!files.length) {
      this.dialogRef.close();

      return;
    }

    this.createdSystemId.set(created.systemId);
    this.uploadService.upload(created.systemId, files);
  }

  private stageRelations(result: LinkTaskDialogResult) {
    const label = this.relationLabel(result.relationType, result.isForward);

    this.stagedRelations.update((staged) => {
      const keyed = new Set(
        staged.map((relation) => this.relationKey(relation))
      );
      const added = result.tasks
        .map((task) => ({
          relationTypeId: result.relationTypeId,
          label,
          taskIsSource: result.isForward,
          task,
        }))
        .filter((relation) => !keyed.has(this.relationKey(relation)));

      return [...staged, ...added];
    });
  }

  private relationLabel(relationType: RelationType, taskIsSource: boolean) {
    return taskIsSource ? relationType.name : relationType.inverseName;
  }

  private relationKey(relation: StagedRelation) {
    return `${relation.relationTypeId}:${relation.task.id}`;
  }

  private buildRequest(projectId: number): AddProjectTaskRequest {
    const { name, description } = this.taskFormModel();
    const assigneeIds = this.assignees().map((assignee) => assignee.id);
    const estimateType = this.estimateType();
    const estimateValue = this.estimateValue();
    const priority = this.priority();
    const statusId = this.statusId();
    const tags = this.selectedTags();
    const relations = this.stagedRelations().map<AddTaskRelationRequest>(
      (relation) => ({
        relatedSystemId: relation.task.systemId,
        relationTypeId: relation.relationTypeId,
        taskIsSource: relation.taskIsSource,
      })
    );

    const task: AddProjectTaskRequest = {
      name: name.trim(),
      description: description.trim(),
      projectId,
      sprintId: this.sprintId(),
      startDate: this.startDate() || null,
      dueDate: this.dueDate() || null,
    };

    if (statusId !== null) task.statusId = statusId;
    if (priority !== null) task.priority = priority;
    if (assigneeIds.length) task.assigneeIds = assigneeIds;
    if (tags.length) task.tags = tags;
    if (relations.length) task.relations = relations;

    if (estimateType !== null) {
      task.estimateType = estimateType;
      if (estimateValue !== null) task.estimateValue = estimateValue;
    }

    return task;
  }
}
