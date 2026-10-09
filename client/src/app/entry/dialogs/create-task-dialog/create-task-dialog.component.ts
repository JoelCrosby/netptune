import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
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
  disabled,
  form,
  maxLength,
  required,
  submit,
  validate,
} from '@angular/forms/signals';
import {
  EditorComponent,
  EditorUploader,
} from '@app/static/components/editor/editor.component';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { EstimateType, TaskEstimate } from '@core/enums/estimate-type';
import { TaskPriority } from '@core/enums/task-priority';
import {
  AddProjectTaskRequest,
  TASK_NAME_MAX_LENGTH,
} from '@core/models/project-task';
import { RelationType } from '@core/models/relation-type';
import { AddTaskRelationRequest } from '@core/models/task-relation';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { UserSelectValue } from '@core/models/view-models/user-select-option';
import { WorkspaceFileContentTypeGroup } from '@core/models/view-models/workspace-file-view-model';
import { CurrentProjectService } from '@core/services/current-project.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { DialogService } from '@core/services/dialog.service';
import { SessionService } from '@core/services/session.service';
import { StorageService } from '@core/services/storage.service';
import { TaskCommandsService } from '@core/services/task-commands.service';
import { TaskFileUploadService } from '@core/services/task-file-upload.service';
import { unwrapClientResponse } from '@core/util/rxjs-operators';
import { firstValueFrom } from 'rxjs';
import { LucideX } from '@lucide/angular';
import { SectionLabelDirective } from '@static/directives/section-label.directive';
import { ListRowComponent } from '@static/components/list-row.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { FileDropzoneComponent } from '@static/components/file-dropzone/file-dropzone.component';
import { FileTypeIconComponent } from '@static/components/file-type-icon/file-type-icon.component';
import { FormErrorsComponent } from '@static/components/form-error/form-errors.component';
import { CharacterLimitComponent } from '@static/components/character-limit/character-limit.component';
import { FileSizePipe } from '@static/pipes/file-size.pipe';
import { TaskTagRowComponent } from '../task-detail-dialog/pickers/task-tag-row.component';
import {
  TaskRelationListComponent,
  TaskRelationListItem,
} from '../task-detail-dialog/parts/task-relation-list.component';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { DividerComponent } from '@static/components/divider/divider.component';
import {
  TabGroupComponent,
  type TabItem,
} from '@static/components/tab-group/tab-group.component';
import { DialogColumnsComponent } from '@static/components/dialog/dialog-columns.component';
import { DialogFooterComponent } from '@static/components/dialog/dialog-footer.component';
import { DialogHeaderComponent } from '@static/components/dialog/dialog-header.component';
import { DialogSectionComponent } from '@static/components/dialog/dialog-section.component';
import { HeadingInputDirective } from '@static/components/form-input/heading-input.directive';
import { UploadProgressComponent } from '@static/components/upload-progress/upload-progress.component';
import { CreateTaskPropertyChipsComponent } from './create-task-property-chips.component';
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

type Tab = 'links' | 'files';

interface CreateTaskReporter {
  displayName: string;
  pictureUrl?: string | null;
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
    AvatarComponent,
    CreateTaskPropertyChipsComponent,
    DialogColumnsComponent,
    DialogFooterComponent,
    DialogHeaderComponent,
    DialogSectionComponent,
    DividerComponent,
    EditorComponent,
    FileDropzoneComponent,
    FileSizePipe,
    FileTypeIconComponent,
    FlatButtonComponent,
    FormErrorsComponent,
    CharacterLimitComponent,
    FormField,
    HeadingInputDirective,
    IconButtonComponent,
    ListRowComponent,
    LucideX,
    SectionLabelDirective,
    StrokedButtonComponent,
    TabGroupComponent,
    TaskRelationListComponent,
    TaskTagRowComponent,
    UploadProgressComponent,
  ],
  providers: [TaskFileUploadService],
  host: { class: 'block h-full min-h-0' },
  template: `
    <form
      class="flex h-full min-h-0 flex-col"
      id="create-task-form"
      novalidate
      (submit)="saveClicked($event)">
      <app-dialog-header
        showCloseButton
        i18n-heading="Title of the create-task dialog"
        heading="Create Task" />

      <div
        class="border-foreground/8 flex shrink-0 flex-col gap-3.5 border-b px-7 pt-5.5 pb-4">
        <div>
          <input
            appHeadingInput
            class="text-[22px]/[30px] tracking-[-0.01em] md:text-[27px]/[34px]"
            type="text"
            autocomplete="off"
            i18n-placeholder="Placeholder in the empty task summary field"
            placeholder="Task summary"
            i18n-aria-label="Label of the task title field"
            aria-label="Summary"
            [formField]="taskForm.name" />
          <app-form-errors [formField]="taskForm.name" />
          <app-character-limit
            [length]="taskForm.name().value().trim().length"
            [max]="nameMaxLength" />
        </div>

        <div class="flex flex-wrap items-center gap-2">
          <app-create-task-property-chips
            [editable]="!busy()"
            [showProject]="!data?.projectId"
            [showSprint]="!data?.sprintId"
            [projectInvalid]="projectInvalid()"
            [estimateType]="estimateType()"
            [estimateValue]="estimateValue()"
            [(statusId)]="statusId"
            [(priority)]="priority"
            [(projectId)]="projectId"
            [(sprintId)]="sprintId"
            [(startDate)]="startDate"
            [(dueDate)]="dueDate"
            [(assignees)]="assignees"
            (estimateChange)="setEstimate($event)" />

          @if (canAssignTags()) {
            <app-divider
              orientation="vertical"
              class="bg-foreground/8 mx-1 h-5" />
            <app-task-tag-row
              size="md"
              [tags]="selectedTags()"
              [editable]="!busy()"
              (added)="addTag($event)"
              (removed)="removeTag($event)" />
          }
        </div>

        @if (scheduleInvalid() || projectInvalid()) {
          <div class="text-warn flex flex-col gap-1 text-xs" role="alert">
            @if (scheduleInvalid()) {
              <p>
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
              <p>
                <span
                  i18n="
                    Validation error when no project is selected for a task
                  ">
                  Project is required.
                </span>
              </p>
            }
          </div>
        }
      </div>

      <app-dialog-columns bodyClass="px-7 pt-5.5 pb-6">
        <div>
          <div
            appSectionLabel
            variant="eyebrow"
            class="mb-2.5"
            id="create-task-description-label">
            {{ labels.description }}
          </div>
          <app-editor
            aria-labelledby="create-task-description-label"
            appearance="flat"
            hostClass="text-[15px]/[26px]"
            i18n-placeholder="Placeholder in the empty task description editor"
            placeholder="Add a Description..."
            [formField]="taskForm.description"
            [uploader]="uploadMedia"
            [isReadOnly]="busy()" />
          <app-form-errors [formField]="taskForm.description" />
        </div>

        @if (tabItems().length) {
          <app-dialog-section
            dialogColumnsFooter
            divider="top"
            class="shrink-0">
            <div class="flex h-[52px] items-center gap-1 px-5">
              <app-tab-group
                variant="island"
                [tabs]="tabItems()"
                [(value)]="activeTab" />

              <div class="ml-auto">
                @if (activeTab() === 'links' && canLinkTasks()) {
                  <button
                    type="button"
                    [class]="tabActionClass"
                    [disabled]="busy()"
                    (click)="openLinkDialog()">
                    <span i18n="Button that links this task to another">
                      Link task
                    </span>
                  </button>
                }
              </div>
            </div>

            <div class="max-h-56 overflow-y-auto px-5 pt-4 pb-4">
              @if (canLinkTasks()) {
                <div [class.hidden]="activeTab() !== 'links'">
                  <app-task-relation-list
                    removable
                    [items]="relationItems()"
                    [disabled]="busy()"
                    (removed)="removeRelation($event)" />
                </div>
              }

              @if (canUploadFiles()) {
                <div [class.hidden]="activeTab() !== 'files'">
                  <app-file-dropzone
                    [disabled]="busy()"
                    [maxBytes]="maxUploadBytes()"
                    (filesSelected)="addFiles($event)" />

                  <ul class="mt-3 flex flex-col gap-2">
                    @for (file of stagedFiles(); track file.name + file.size) {
                      <li app-list-row>
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
                            Accessible label for the button that takes a file
                            off a task that has not been created yet
                          "
                          aria-label="Remove file"
                          (click)="removeFile(file)">
                          <svg lucideX class="h-4 w-4"></svg>
                        </button>
                      </li>
                    }
                  </ul>

                  <div class="mt-2 flex flex-col gap-2" aria-live="polite">
                    @for (upload of uploads(); track upload.id) {
                      <app-upload-progress
                        [name]="upload.name"
                        [progress]="upload.progress"
                        [error]="upload.error" />
                    }
                  </div>

                  @if (uploadsFailed()) {
                    <p class="text-warn mt-2 text-sm" role="alert">
                      <span
                        i18n="
                          Shown when a new task was saved but some of its files
                          did not upload
                        ">
                        The task was created, but some files did not upload.
                        Close this dialog and add them from the task.
                      </span>
                    </p>
                  }
                </div>
              }
            </div>
          </app-dialog-section>
        }
      </app-dialog-columns>

      <app-dialog-footer>
        @if (reporter(); as reporter) {
          <span class="text-muted mr-auto flex items-center gap-2 text-xs">
            <app-avatar
              size="xs"
              [tooltip]="false"
              [name]="reporter.displayName"
              [imageUrl]="reporter.pictureUrl" />
            <span
              i18n="
                Footer line naming who raises the new task. NAME is their
                display name
              ">
              Reported by
              {{
                reporter.displayName // i18n(ph="NAME")
              }}
            </span>
          </span>
        }

        <button app-stroked-button type="button" (click)="close()">
          <span i18n="Dismisses a dialog without saving">Close</span>
        </button>
        <button
          app-flat-button
          color="primary"
          type="submit"
          form="create-task-form"
          [disabled]="busy()">
          <span i18n="Button that saves the new task">Save Task</span>
        </button>
      </app-dialog-footer>
    </form>
  `,
})
export class CreateTaskDialogComponent {
  static readonly width = '1140px';
  static readonly height = '740px';
  static readonly panelClass = 'app-task-detail-dialog';

  private taskCommands = inject(TaskCommandsService);
  private dialog = inject(DialogService);
  private session = inject(SessionService);
  private uploadService = inject(TaskFileUploadService);
  private storage = inject(StorageService);
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

  // the task does not exist yet, so the server links description media to it on create.
  readonly uploadMedia: EditorUploader = (file) => {
    return firstValueFrom(
      this.storage.uploadMedia(file).pipe(unwrapClientResponse())
    );
  };
  readonly canLinkTasks = hasPermission(PERMISSIONS.tasks.update);
  readonly readStatus = hasPermission(PERMISSIONS.statuses.read);

  readonly labels = {
    description: $localize`:Label of the task description editor:Description`,
    links: $localize`:Tab listing the tasks this one links to:Links`,
    files: $localize`:Section heading for files attached to a task:Files`,
  };

  readonly tabActionClass =
    'border-foreground/8 hover:bg-hover h-[30px] cursor-pointer rounded-[7px] border px-2.5 text-xs font-medium transition-colors disabled:pointer-events-none disabled:opacity-50';

  // Picking tags means both listing the workspace's tags and being allowed to attach one.
  readonly canAssignTags = computed(() => {
    return this.canReadTags() && this.canAssignTagsToTasks();
  });

  // Whoever opens the dialog becomes the task's reporter, so it reads the same as the detail dialog.
  readonly reporter = computed<CreateTaskReporter | null>(() => {
    const user = this.session.currentUser();

    if (!user) return null;

    return {
      displayName: user.displayName || user.email,
      pictureUrl: user.pictureUrl,
    };
  });

  readonly selectedTags = signal<string[]>([]);
  readonly stagedFiles = signal<File[]>([]);
  readonly stagedRelations = signal<StagedRelation[]>([]);
  readonly uploads = this.uploadService.uploads;

  readonly activeTab = signal<Tab>(this.canLinkTasks() ? 'links' : 'files');

  readonly tabItems = computed<TabItem[]>(() => {
    const tabs: TabItem[] = [];

    if (this.canLinkTasks()) {
      tabs.push({
        value: 'links',
        label: this.labels.links,
        count: this.stagedRelations().length,
      });
    }

    if (this.canUploadFiles()) {
      tabs.push({
        value: 'files',
        label: this.labels.files,
        count: this.stagedFiles().length,
      });
    }

    return tabs;
  });

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

  readonly relationItems = computed<TaskRelationListItem[]>(() => {
    return this.stagedRelations().map((relation) => ({
      id: this.relationKey(relation),
      label: relation.label,
      systemId: relation.task.systemId,
      name: relation.task.name,
      statusName: relation.task.statusName,
      statusColor: relation.task.statusColor,
    }));
  });

  readonly nameMaxLength = TASK_NAME_MAX_LENGTH;

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

      if (name.length > TASK_NAME_MAX_LENGTH) {
        return {
          kind: 'maxLength',
          message: $localize`:Body of a dialog or validation message:Summary cannot exceed ${TASK_NAME_MAX_LENGTH}:maxLength: characters.`,
        };
      }

      return undefined;
    });
    maxLength(schema.description, 4096, {
      message: $localize`:Body of a dialog or validation message:Description cannot exceed 4096 characters.`,
    });
    disabled(schema.name, { when: () => this.busy() });
    disabled(schema.description, { when: () => this.busy() });
  });

  constructor() {
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

  addTag(tag: string) {
    this.selectedTags.update((tags) => [...tags, tag]);
  }

  removeTag(tag: string) {
    this.selectedTags.update((tags) => tags.filter((item) => item !== tag));
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

  async openLinkDialog() {
    const result = await this.dialog.openForResult<
      LinkTaskDialogResult,
      LinkTaskDialogData
    >(LinkTaskDialogComponent, {
      data: {},
      width: '1100px',
      panelClass: LinkTaskDialogComponent.panelClass,
    });

    if (!result) return;

    this.stageRelations(result);
  }

  removeRelation(item: TaskRelationListItem) {
    this.stagedRelations.update((staged) => {
      return staged.filter(
        (relation) => this.relationKey(relation) !== item.id
      );
    });
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

    // The uploads report their progress on the files tab.
    this.activeTab.set('files');
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
