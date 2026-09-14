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
import { EditorComponent } from '@app/static/components/editor/editor.component';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { EstimateType, TaskEstimate } from '@core/enums/estimate-type';
import { TaskPriority } from '@core/enums/task-priority';
import { AddProjectTaskRequest } from '@core/models/project-task';
import { RelationType } from '@core/models/relation-type';
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
import { LucideX } from '@lucide/angular';
import { SectionLabelDirective } from '@static/directives/section-label.directive';
import { ListRowComponent } from '@static/components/list-row.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { FileDropzoneComponent } from '@static/components/file-dropzone/file-dropzone.component';
import { FileTypeIconComponent } from '@static/components/file-type-icon/file-type-icon.component';
import { FormErrorsComponent } from '@static/components/form-error/form-errors.component';
import { FileSizePipe } from '@static/pipes/file-size.pipe';
import { TaskStatusSegmentsComponent } from '../task-detail-dialog/pickers/task-status-segments.component';
import { TaskTagRowComponent } from '../task-detail-dialog/pickers/task-tag-row.component';
import { TaskFilesSectionComponent } from '../task-detail-dialog/parts/task-files-section.component';
import { TaskLinksSectionComponent } from '../task-detail-dialog/parts/task-links-section.component';
import {
  TaskRelationListComponent,
  TaskRelationListItem,
} from '../task-detail-dialog/parts/task-relation-list.component';
import { AccordionComponent } from '@static/components/accordion/accordion.component';
import { DialogColumnsComponent } from '@static/components/dialog/dialog-columns.component';
import { DialogFooterComponent } from '@static/components/dialog/dialog-footer.component';
import { DialogHeaderComponent } from '@static/components/dialog/dialog-header.component';
import { DialogRailComponent } from '@static/components/dialog/dialog-rail.component';
import { DialogSectionComponent } from '@static/components/dialog/dialog-section.component';
import { HeadingInputDirective } from '@static/components/form-input/heading-input.directive';
import { UploadProgressComponent } from '@static/components/upload-progress/upload-progress.component';
import {
  CreateTaskFieldRowsComponent,
  CreateTaskReporter,
} from './create-task-field-rows.component';
import {
  LinkTaskDialogComponent,
  LinkTaskDialogData,
  LinkTaskDialogResult,
} from '../link-task-dialog/link-task-dialog.component';
import { toggleInSet } from '@core/util/signals';

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

type Section = 'links' | 'files';

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
    AccordionComponent,
    CreateTaskFieldRowsComponent,
    DialogColumnsComponent,
    DialogFooterComponent,
    DialogHeaderComponent,
    DialogRailComponent,
    DialogSectionComponent,
    EditorComponent,
    FileDropzoneComponent,
    FileSizePipe,
    FileTypeIconComponent,
    FlatButtonComponent,
    FormErrorsComponent,
    FormField,
    HeadingInputDirective,
    IconButtonComponent,
    ListRowComponent,
    LucideX,
    SectionLabelDirective,
    StrokedButtonComponent,
    TaskFilesSectionComponent,
    TaskLinksSectionComponent,
    TaskRelationListComponent,
    TaskStatusSegmentsComponent,
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

      <app-dialog-columns>
        <div>
          <input
            appHeadingInput
            type="text"
            autocomplete="off"
            i18n-placeholder="Placeholder in the empty task summary field"
            placeholder="Task summary"
            i18n-aria-label="Label of the task title field"
            aria-label="Summary"
            [formField]="taskForm.name" />
          <app-form-errors [formField]="taskForm.name" />
        </div>

        @if (canAssignTags()) {
          <app-task-tag-row
            [tags]="selectedTags()"
            [editable]="!busy()"
            (added)="addTag($event)"
            (removed)="removeTag($event)" />
        }

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
            [isReadOnly]="busy()" />
          <app-form-errors [formField]="taskForm.description" />
        </div>

        @if (canLinkTasks() || canUploadFiles()) {
          <app-accordion class="mt-auto">
            @if (canLinkTasks()) {
              <app-task-links-section
                canLink
                [count]="stagedRelations().length"
                [last]="!canUploadFiles()"
                [disabled]="busy()"
                [expanded]="isExpanded('links')"
                (toggled)="toggle('links')"
                (linkRequested)="openLinkDialog()">
                <app-task-relation-list
                  removable
                  [items]="relationItems()"
                  [disabled]="busy()"
                  (removed)="removeRelation($event)" />
              </app-task-links-section>
            }

            @if (canUploadFiles()) {
              <app-task-files-section
                last
                [count]="stagedFiles().length"
                [expanded]="isExpanded('files')"
                (toggled)="toggle('files')"
                (chooseRequested)="expand('files')">
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
                          Accessible label for the button that takes a file off
                          a task that has not been created yet
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
                      The task was created, but some files did not upload. Close
                      this dialog and add them from the task.
                    </span>
                  </p>
                }
              </app-task-files-section>
            }
          </app-accordion>
        }

        <app-dialog-rail>
          @if (readStatus()) {
            <app-dialog-section divider="bottom" class="px-5 pt-4.5 pb-4">
              <app-task-status-segments
                [eyebrowId]="statusEyebrowId"
                [disabled]="busy()"
                [(value)]="statusId" />
            </app-dialog-section>
          }

          <app-create-task-field-rows
            class="custom-scroll min-h-0 flex-1 overflow-y-auto px-2 pt-2 pb-4"
            [editable]="!busy()"
            [reporter]="reporter()"
            [showProject]="!data?.projectId"
            [showSprint]="!data?.sprintId"
            [estimateType]="estimateType()"
            [estimateValue]="estimateValue()"
            [(priority)]="priority"
            [(projectId)]="projectId"
            [(sprintId)]="sprintId"
            [(startDate)]="startDate"
            [(dueDate)]="dueDate"
            [(assignees)]="assignees"
            (estimateChange)="setEstimate($event)" />

          @if (scheduleInvalid() || projectInvalid()) {
            <app-dialog-section
              divider="top"
              class="text-warn shrink-0 px-4 py-3 text-xs"
              role="alert">
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
            </app-dialog-section>
          }
        </app-dialog-rail>
      </app-dialog-columns>

      <app-dialog-footer>
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
  readonly readStatus = hasPermission(PERMISSIONS.statuses.read);

  readonly statusEyebrowId = 'create-task-status-eyebrow';

  readonly labels = {
    description: $localize`:Label of the task description editor:Description`,
  };

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

  private readonly expanded = signal<ReadonlySet<Section>>(new Set());

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

    // A staged file or link is invisible while its section is folded, so adding one opens it.
    effect(() => {
      if (!this.stagedFiles().length) return;

      untracked(() => this.expand('files'));
    });

    effect(() => {
      if (!this.stagedRelations().length) return;

      untracked(() => this.expand('links'));
    });
  }

  isExpanded(section: Section) {
    return this.expanded().has(section);
  }

  toggle(section: Section) {
    toggleInSet(this.expanded, section);
  }

  expand(section: Section) {
    toggleInSet(this.expanded, section, true);
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
