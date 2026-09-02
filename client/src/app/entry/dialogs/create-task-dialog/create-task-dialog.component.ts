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
import { LucideLink2, LucideX } from '@lucide/angular';
import { ColorSwatchComponent } from '@static/components/color-swatch/color-swatch.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { FileDropzoneComponent } from '@static/components/file-dropzone/file-dropzone.component';
import { FileTypeIconComponent } from '@static/components/file-type-icon/file-type-icon.component';
import { FormErrorsComponent } from '@static/components/form-error/form-errors.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { FileSizePipe } from '@static/pipes/file-size.pipe';
import { TaskDetailAccordionRowComponent } from '../task-detail-dialog/shared/task-detail-accordion-row.component';
import { TaskStatusSegmentsComponent } from '../task-detail-dialog/pickers/task-status-segments.component';
import { TaskTagRowComponent } from '../task-detail-dialog/pickers/task-tag-row.component';
import {
  EYEBROW,
  HEADER_ICON_BUTTON,
} from '../task-detail-dialog/task-detail-styles';
import {
  CreateTaskFieldRowsComponent,
  CreateTaskReporter,
} from './create-task-field-rows.component';
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
    ColorSwatchComponent,
    CreateTaskFieldRowsComponent,
    FormField,
    FileDropzoneComponent,
    FileSizePipe,
    FileTypeIconComponent,
    FlatButtonComponent,
    IconButtonComponent,
    StrokedButtonComponent,
    EditorComponent,
    FormErrorsComponent,
    LucideLink2,
    LucideX,
    TaskDetailAccordionRowComponent,
    TaskScopeIdComponent,
    TaskStatusSegmentsComponent,
    TaskTagRowComponent,
    TooltipDirective,
  ],
  providers: [TaskFileUploadService],
  host: { class: 'block h-full min-h-0' },
  template: `
    <form
      class="flex h-full min-h-0 flex-col"
      id="create-task-form"
      novalidate
      (submit)="saveClicked($event)">
      <div
        class="border-foreground/8 flex h-[50px] shrink-0 items-center gap-2.5 border-b pr-3.5 pl-5">
        <span class="text-[13px] font-semibold">
          <span i18n="Title of the create-task dialog">Create Task</span>
        </span>

        <div class="ml-auto flex shrink-0 items-center gap-0.5">
          <button
            type="button"
            [class]="iconButtonClass"
            i18n-aria-label="
              Accessible label for the button that closes a dialog
            "
            aria-label="Close"
            (click)="close()">
            <svg lucideX class="h-4 w-4"></svg>
          </button>
        </div>
      </div>

      <div class="flex min-h-0 flex-1 flex-row max-[1200px]:flex-col">
        <div class="flex min-w-0 flex-1 flex-col">
          <div
            class="custom-scroll flex min-h-0 flex-1 flex-col gap-[18px] overflow-y-auto px-7 pt-6 pb-5">
            <div>
              <input
                class="placeholder:text-muted -mx-2 w-full rounded bg-transparent px-2 py-1 text-[28px]/[36px] font-semibold tracking-[-0.012em] transition-colors outline-none hover:bg-black/5 focus:bg-black/5 dark:hover:bg-white/5 dark:focus:bg-white/5"
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
              <div [class]="eyebrowClass" id="create-task-description-label">
                {{ labels.description }}
              </div>
              <app-editor
                aria-labelledby="create-task-description-label"
                appearance="flat"
                hostClass="text-[15px]/[26px]"
                i18n-placeholder="
                  Placeholder in the empty task description editor
                "
                placeholder="Add a Description..."
                [formField]="taskForm.description"
                [isReadOnly]="busy()" />
              <app-form-errors [formField]="taskForm.description" />
            </div>

            @if (canLinkTasks() || canUploadFiles()) {
              <div class="border-foreground/8 mt-auto flex flex-col border-t">
                @if (canLinkTasks()) {
                  <app-task-detail-accordion-row
                    [label]="labels.links"
                    [summary]="linkSummary()"
                    [last]="!canUploadFiles()"
                    [expanded]="isExpanded('links')"
                    (toggled)="toggle('links')">
                    <button
                      type="button"
                      class="text-primary hover:bg-hover shrink-0 cursor-pointer rounded px-2 py-1 text-xs font-medium transition-colors"
                      [disabled]="busy()"
                      (click)="openLinkDialog()">
                      <span i18n="Button that links this task to another">
                        Link task
                      </span>
                    </button>
                  </app-task-detail-accordion-row>

                  <div class="pt-1 pb-3" [class.hidden]="!isExpanded('links')">
                    @for (group of relationGroups(); track group.label) {
                      <div class="mb-3">
                        <div
                          class="text-muted mb-1 text-xs font-medium tracking-wide uppercase">
                          {{ group.label }}
                        </div>

                        <ul class="flex flex-col gap-1">
                          @for (
                            relation of group.relations;
                            track relation.task.id
                          ) {
                            <li
                              class="border-foreground/8 bg-foreground/[0.02] flex items-center gap-3 rounded-lg border px-3 py-2">
                              <app-color-swatch
                                size="sm"
                                [color]="relation.task.statusColor" />

                              <app-task-scope-id
                                [id]="relation.task.systemId" />

                              <span class="flex-1 truncate">
                                {{ relation.task.name }}
                              </span>

                              <span class="text-muted shrink-0 text-xs">
                                {{ relation.task.statusName }}
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
                                  Accessible label for the button that removes a
                                  task link
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
                          i18n="
                            Empty state when a task has no links to other tasks
                          ">
                          No linked tasks
                        </span>
                      </div>
                    }
                  </div>
                }

                @if (canUploadFiles()) {
                  <app-task-detail-accordion-row
                    [label]="labels.files"
                    [summary]="fileSummary()"
                    [last]="true"
                    [expanded]="isExpanded('files')"
                    (toggled)="toggle('files')">
                    <button
                      type="button"
                      class="text-primary hover:bg-hover shrink-0 cursor-pointer rounded px-2 py-1 text-xs font-medium transition-colors"
                      (click)="expand('files')">
                      <span i18n="Button that opens the file picker">
                        Choose files
                      </span>
                    </button>
                  </app-task-detail-accordion-row>

                  <div class="pt-1 pb-3" [class.hidden]="!isExpanded('files')">
                    <app-file-dropzone
                      [disabled]="busy()"
                      [maxBytes]="maxUploadBytes()"
                      (filesSelected)="addFiles($event)" />

                    <div class="mt-3 flex flex-col gap-2">
                      @for (
                        file of stagedFiles();
                        track file.name + file.size
                      ) {
                        <div
                          class="border-foreground/8 bg-foreground/[0.02] flex items-center gap-3 rounded-lg border px-3 py-2">
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
                          <div
                            class="bg-muted mt-1 h-1 overflow-hidden rounded">
                            <div
                              class="bg-primary h-full"
                              [style.width.%]="upload.progress"></div>
                          </div>
                        </div>
                      }
                    </div>

                    @if (uploadsFailed()) {
                      <p class="text-warn mt-2 text-sm" role="alert">
                        <span
                          i18n="
                            Shown when a new task was saved but some of its
                            files did not upload
                          ">
                          The task was created, but some files did not upload.
                          Close this dialog and add them from the task.
                        </span>
                      </p>
                    }
                  </div>
                }
              </div>
            }
          </div>
        </div>

        <div
          class="border-foreground/8 bg-foreground/[0.02] flex w-[340px] shrink-0 flex-col border-l max-[1200px]:w-full max-[1200px]:border-t max-[1200px]:border-l-0">
          @if (readStatus()) {
            <app-task-status-segments
              class="border-foreground/8 border-b px-5 pt-4.5 pb-4"
              [eyebrowId]="statusEyebrowId"
              [disabled]="busy()"
              [(value)]="statusId" />
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
            <div
              class="border-foreground/8 text-warn shrink-0 border-t px-4 py-3 text-xs"
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
            </div>
          }
        </div>
      </div>

      <div
        class="border-foreground/8 bg-foreground/[0.02] flex shrink-0 items-center justify-end gap-2 border-t px-5 py-3">
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
      </div>
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

  readonly iconButtonClass = HEADER_ICON_BUTTON;
  readonly statusEyebrowId = 'create-task-status-eyebrow';
  readonly eyebrowClass = `${EYEBROW} mb-2.5`;

  readonly labels = {
    description: $localize`:Label of the task description editor:Description`,
    links: $localize`:Section heading for links between tasks:Linked tasks`,
    files: $localize`:Section heading for files attached to a task:Files`,
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

  private readonly expanded = signal(new Set<Section>());

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
    this.expanded.update((sections) => {
      const next = new Set(sections);

      if (!next.delete(section)) next.add(section);

      return next;
    });
  }

  expand(section: Section) {
    this.expanded.update((sections) => new Set(sections).add(section));
  }

  linkSummary() {
    const count = this.stagedRelations().length;

    if (!count) {
      return $localize`:Accordion summary when a task links to nothing:None yet`;
    }

    if (count === 1) {
      return $localize`:Accordion summary when a task links to one other:1 linked task`;
    }

    return $localize`:Accordion summary counting the tasks this one links to. COUNT is how many:${count}:COUNT: linked tasks`;
  }

  fileSummary() {
    const count = this.stagedFiles().length;

    if (!count) {
      return $localize`:Prompt on the collapsed files section:Drop a file to attach`;
    }

    if (count === 1) {
      return $localize`:Accordion summary when a task has one attachment:1 file`;
    }

    return $localize`:Accordion summary counting a task's attachments. COUNT is how many:${count}:COUNT: files`;
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
