import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import {
  Component,
  ElementRef,
  Injector,
  LOCALE_ID,
  afterNextRender,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { BulkCollectionMode } from '@core/enums/bulk-collection-mode';
import { EstimateType, estimateTypeOptions } from '@core/enums/estimate-type';
import {
  TaskPriority,
  taskPriorityColors,
  taskPriorityOptions,
} from '@core/enums/task-priority';
import { BulkUpdateTasksRequest } from '@core/models/requests/bulk-update-tasks-request';
import { projectResource } from '@core/resources/project.resource';
import { sprintResource } from '@core/resources/sprint.resource';
import { statusResource } from '@core/resources/status.resource';
import { tagResource } from '@core/resources/tag.resource';
import { userResource } from '@core/resources/user.resource';
import { TaskCommandsService } from '@core/services/task-commands.service';
import { LucideFlag, LucidePlus } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DatePickerComponent } from '@static/components/date-picker/date-picker.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { NumberInputComponent } from '@static/components/number-input/number-input.component';
import {
  BulkEditCollectionPickerComponent,
  BulkEditPickerOption,
} from './bulk-edit-collection-picker.component';
import {
  BulkEditFieldKey,
  bulkEditFieldTypeLabels,
  bulkEditFields,
} from './bulk-edit-fields';
import { BulkEditRowComponent } from './bulk-edit-row.component';
import { BulkEditTask } from './bulk-edit-task';
import {
  assigneesHint,
  dueDateHint,
  estimateTypeHint,
  estimateValueHint,
  priorityHint,
  projectHint,
  sprintHint,
  statusHint,
  tagsHint,
} from './bulk-edit-summary';

const NO_SPRINT = -1;

const fadeEdgePx = 4;

@Component({
  selector: 'app-bulk-edit-tasks-dialog',
  imports: [
    BulkEditCollectionPickerComponent,
    BulkEditRowComponent,
    DatePickerComponent,
    DropdownMenuComponent,
    FlatButtonComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    LucideFlag,
    LucidePlus,
    MenuItemComponent,
    NumberInputComponent,
    StrokedButtonComponent,
  ],
  host: { class: 'flex min-h-0 flex-auto flex-col' },
  template: `
    <div class="flex flex-none items-baseline justify-between gap-4">
      <h1
        class="m-0 text-xl font-medium"
        i18n="Title of the dialog for editing several tasks at once">
        Bulk edit tasks
      </h1>
      <span class="text-muted text-[13px] whitespace-nowrap">
        <ng-container i18n="Count of the tasks a bulk edit will be applied to">
          {taskCount, plural,
            =1 {1 task selected}
            other {{{ taskCount }} tasks selected}
          }
        </ng-container>
      </span>
    </div>

    <p
      class="text-muted mt-1 mb-3 flex-none text-sm"
      i18n="Explains that a bulk edit only touches the fields that were added">
      Only the fields you add below are written. Everything else is left alone.
    </p>

    <div class="relative -mr-2.5 flex min-h-0 flex-auto flex-col">
      <div
        class="from-dialog-background pointer-events-none absolute top-0 right-2.5 left-0 z-4 h-4 bg-gradient-to-b to-transparent transition-opacity"
        [class.opacity-0]="!showTopFade()"></div>
      <div
        class="from-dialog-background pointer-events-none absolute right-2.5 bottom-0 left-0 z-4 h-5 bg-gradient-to-t to-transparent transition-opacity"
        [class.opacity-0]="!showBottomFade()"></div>

      <div
        #rows
        class="custom-scroll flex min-h-0 flex-auto flex-col gap-6.5 overflow-x-hidden overflow-y-auto py-0.5 pr-1"
        (scroll)="onRowsScroll($event)">
        @for (field of addedFields(); track field.key) {
          <app-bulk-edit-row
            [label]="field.label"
            [controlId]="controlId(field.key)"
            [hint]="hints().get(field.key)"
            [removeLabel]="removeLabelFor(field.label)"
            (removed)="removeField(field.key)">
            @switch (field.key) {
              @case ('status') {
                <app-form-select
                  [noMargin]="true"
                  label=""
                  [name]="controlId('status')"
                  [(value)]="statusId">
                  @for (status of statuses.value(); track status.id) {
                    <app-form-select-option [value]="status.id">
                      {{ status.name }}
                    </app-form-select-option>
                  }
                </app-form-select>
              }

              @case ('priority') {
                <app-form-select
                  [noMargin]="true"
                  label=""
                  [name]="controlId('priority')"
                  [(value)]="priority">
                  @for (option of priorityOptions; track option.value) {
                    <app-form-select-option [value]="option.value">
                      <span class="flex items-center gap-2">
                        <svg
                          lucideFlag
                          class="h-3.5 w-3.5"
                          [class]="priorityColor(option.value)"></svg>
                        {{ option.label }}
                      </span>
                    </app-form-select-option>
                  }
                </app-form-select>
              }

              @case ('dueDate') {
                <div class="flex items-center gap-2">
                  <app-date-picker
                    class="min-w-0 flex-1"
                    [controlId]="controlId('dueDate')"
                    [disabled]="clearDueDate()"
                    [ariaLabel]="field.label"
                    [(value)]="dueDate" />

                  <button
                    type="button"
                    class="text-primary hover:bg-primary/10 h-10 shrink-0 cursor-pointer rounded-sm px-3 text-[13px] font-medium transition-colors"
                    (click)="toggleClearDueDate()">
                    @if (clearDueDate()) {
                      <span
                        i18n="
                          Button that goes back to picking a due date instead of
                          clearing it
                        ">
                        Pick a date instead
                      </span>
                    } @else {
                      <span
                        i18n="
                          Button that clears the due date instead of setting one
                        ">
                        Clear instead
                      </span>
                    }
                  </button>
                </div>
              }

              @case ('estimateType') {
                <app-form-select
                  [noMargin]="true"
                  label=""
                  [name]="controlId('estimateType')"
                  [(value)]="estimateType">
                  @for (option of estimateOptions; track option.value) {
                    <app-form-select-option [value]="option.value">
                      {{ option.label }}
                    </app-form-select-option>
                  }
                </app-form-select>
              }

              @case ('estimateValue') {
                <app-number-input
                  class="w-38"
                  [min]="0"
                  [name]="controlId('estimateValue')"
                  [(value)]="estimateValue" />
              }

              @case ('tags') {
                <app-bulk-edit-collection-picker
                  [options]="tagOptions()"
                  [selected]="tags()"
                  [mode]="tagMode()"
                  [searchPlaceholder]="labels.searchTags"
                  [listAriaLabel]="field.label"
                  [modeAriaLabel]="labels.tagModeLabel"
                  [emptyMessage]="labels.noTags"
                  (toggled)="toggleTag($event)"
                  (cleared)="tags.set([])"
                  (modeChange)="tagMode.set($event)" />
              }

              @case ('assignees') {
                <app-bulk-edit-collection-picker
                  [avatars]="true"
                  [options]="assigneeOptions()"
                  [selected]="assigneeIds()"
                  [mode]="assigneeMode()"
                  [searchPlaceholder]="labels.searchPeople"
                  [listAriaLabel]="field.label"
                  [modeAriaLabel]="labels.assigneeModeLabel"
                  [emptyMessage]="labels.noPeople"
                  (toggled)="toggleAssignee($event)"
                  (cleared)="assigneeIds.set([])"
                  (modeChange)="assigneeMode.set($event)" />
              }

              @case ('project') {
                <app-form-select
                  [noMargin]="true"
                  label=""
                  [name]="controlId('project')"
                  [(value)]="projectId">
                  @for (project of projects.value(); track project.id) {
                    <app-form-select-option [value]="project.id">
                      {{ project.name }}
                    </app-form-select-option>
                  }
                </app-form-select>
              }

              @case ('sprint') {
                <app-form-select
                  [noMargin]="true"
                  label=""
                  [name]="controlId('sprint')"
                  [(value)]="sprintId">
                  <app-form-select-option [value]="noSprint">
                    <span i18n="Option that clears the task's sprint">
                      No sprint
                    </span>
                  </app-form-select-option>
                  @for (sprint of sprints.value(); track sprint.id) {
                    <app-form-select-option [value]="sprint.id">
                      {{ sprint.name }}
                    </app-form-select-option>
                  }
                </app-form-select>
              }
            }
          </app-bulk-edit-row>
        } @empty {
          <p
            class="text-muted py-10 text-center text-sm"
            i18n="Shown in a bulk edit dialog before any field has been added">
            Nothing will change yet. Add a field below to start.
          </p>
        }
      </div>
    </div>

    <div class="relative mt-3.5 flex-none">
      <button
        #addTrigger
        type="button"
        class="border-foreground/24 text-primary hover:border-primary/50 hover:bg-primary/8 inline-flex h-10 cursor-pointer items-center gap-2 rounded-sm border border-dashed px-3.5 text-sm font-medium transition-colors disabled:pointer-events-none disabled:opacity-40"
        [disabled]="!remainingFields().length"
        (click)="addMenu.toggle(addTrigger)">
        <svg lucidePlus class="h-4 w-4" aria-hidden="true"></svg>
        <span i18n="Button that adds another field to a bulk edit">
          Add a field
        </span>
      </button>

      <app-dropdown-menu #addMenu>
        @for (field of remainingFields(); track field.key) {
          <button
            app-menu-item
            type="button"
            (click)="addField(field.key); addMenu.close()">
            <span class="min-w-0 flex-1 truncate">{{ field.label }}</span>
            <span class="text-foreground/40 font-mono text-[11px]">
              {{ typeLabels[field.type] }}
            </span>
          </button>
        }
      </app-dropdown-menu>
    </div>

    <div
      class="border-border mt-4 flex flex-none items-center justify-between gap-4 border-t pt-4">
      <span class="text-muted text-[13px]">
        <ng-container
          i18n="
            Sums up a bulk edit: how many fields it writes, how many tasks it
            touches, and how many of those fields overwrite a value the tasks
            already hold
          ">
          {addedCount(), plural, =1 {1 field} other {{{ addedCount() }} fields}}
          ·
          {taskCount, plural, =1 {1 task} other {{{ taskCount }} tasks}}
          ·
          {overwriteCount(), plural,
            =1 {1 replaces an existing value}
            other {{{ overwriteCount() }} replace existing values}
          }
        </ng-container>
      </span>

      <div class="flex gap-3">
        <button app-stroked-button type="button" (click)="close()">
          <span i18n="Dismisses a dialog without acting">Cancel</span>
        </button>
        <button
          app-flat-button
          type="button"
          [disabled]="!canApply()"
          (click)="apply()">
          <ng-container
            i18n="Button that applies a bulk edit to the selected tasks">
            {addedCount(), plural,
              =1 {Apply 1 change}
              other {Apply {{ addedCount() }} changes}
            }
          </ng-container>
        </button>
      </div>
    </div>
  `,
})
export class BulkEditTasksDialogComponent {
  static readonly width = '760px';
  static readonly panelClass = 'app-bulk-edit-dialog';

  private readonly dialogRef =
    inject<DialogRef<void, BulkEditTasksDialogComponent>>(DialogRef);
  private readonly taskCommands = inject(TaskCommandsService);
  private readonly injector = inject(Injector);
  private readonly locale = inject(LOCALE_ID);

  readonly tasks = inject<BulkEditTask[]>(DIALOG_DATA);
  readonly taskCount = this.tasks.length;

  protected readonly noSprint = NO_SPRINT;
  protected readonly priorityOptions = taskPriorityOptions;
  protected readonly estimateOptions = estimateTypeOptions;
  protected readonly typeLabels = bulkEditFieldTypeLabels;

  protected readonly labels = {
    searchTags: $localize`:Placeholder in the box that searches tags:Search tags`,
    searchPeople: $localize`:Placeholder in the box that searches people:Search people`,
    tagModeLabel: $localize`:Accessible label for the control that chooses whether a bulk edit adds tags or replaces them:Tag change mode`,
    assigneeModeLabel: $localize`:Accessible label for the control that chooses whether a bulk edit adds assignees or replaces them:Assignee change mode`,
    noTags: $localize`:Shown when a workspace has no tags to pick from:No tags`,
    noPeople: $localize`:Shown when a workspace has no people to pick from:No people`,
  };

  readonly statuses = statusResource();
  readonly projects = projectResource();
  readonly sprints = sprintResource();
  readonly users = userResource();
  readonly tagsResource = tagResource();

  private readonly rows = viewChild<ElementRef<HTMLElement>>('rows');

  protected readonly added = signal<BulkEditFieldKey[]>([]);

  protected readonly statusId = signal<number | null>(null);
  protected readonly priority = signal<TaskPriority | null>(TaskPriority.none);
  protected readonly dueDate = signal('');
  protected readonly clearDueDate = signal(false);
  protected readonly estimateType = signal<EstimateType | null>(null);
  protected readonly estimateValue = signal<number | null>(null);
  protected readonly tags = signal<string[]>([]);
  protected readonly tagMode = signal(BulkCollectionMode.add);
  protected readonly assigneeIds = signal<string[]>([]);
  protected readonly assigneeMode = signal(BulkCollectionMode.add);
  protected readonly projectId = signal<number | null>(null);
  protected readonly sprintId = signal<number | null>(NO_SPRINT);

  protected readonly showTopFade = signal(false);
  protected readonly showBottomFade = signal(false);

  private readonly readableFields = computed(() => {
    const readable: Record<BulkEditFieldKey, boolean> = {
      status: this.statuses.canRead(),
      priority: true,
      dueDate: true,
      estimateType: true,
      estimateValue: true,
      tags: this.tagsResource.canRead(),
      assignees: this.users.canRead(),
      project: this.projects.canRead(),
      sprint: this.sprints.canRead(),
    };

    return bulkEditFields.filter((field) => readable[field.key]);
  });

  protected readonly addedFields = computed(() => {
    const added = this.added();

    return this.readableFields().filter((field) => added.includes(field.key));
  });

  protected readonly remainingFields = computed(() => {
    const added = this.added();

    return this.readableFields().filter((field) => !added.includes(field.key));
  });

  protected readonly addedCount = computed(() => this.added().length);

  protected readonly hints = computed(() => {
    const hints = new Map<BulkEditFieldKey, string>();

    for (const field of this.addedFields()) {
      hints.set(field.key, this.hintFor(field.key));
    }

    return hints;
  });

  protected readonly assignableUsers = computed(() => {
    const users = this.users.value()?.payload?.items ?? [];

    return users.filter((user) => !user.isPending);
  });

  protected readonly assigneeOptions = computed<BulkEditPickerOption[]>(() => {
    const assignedCounts = this.assignedCounts();

    return this.assignableUsers().map((user) => {
      return {
        value: user.id,
        label: user.displayName,
        hint: this.selectionHint(assignedCounts.get(user.id) ?? 0),
        pictureUrl: user.pictureUrl,
        isServiceAccount: user.isServiceAccount,
      };
    });
  });

  protected readonly tagOptions = computed<BulkEditPickerOption[]>(() => {
    const taggedCounts = this.taggedCounts();

    return (this.tagsResource.value() ?? []).map((tag) => {
      return {
        value: tag.name,
        label: tag.name,
        hint: this.selectionHint(taggedCounts.get(tag.name) ?? 0),
      };
    });
  });

  private readonly projectNames = computed(() => {
    const names = new Map<number, string>();

    for (const project of this.projects.value() ?? []) {
      names.set(project.id, project.name);
    }

    return names;
  });

  protected readonly overwriteCount = computed(() => {
    return this.added().filter((key) => this.replacesExistingValue(key)).length;
  });

  protected readonly canApply = computed(() => {
    const added = this.added();

    if (!added.length) return false;

    return !added.some((key) => this.isIncomplete(key));
  });

  constructor() {
    effect(() => {
      this.added();

      afterNextRender(() => this.measureRows(), { injector: this.injector });
    });
  }

  close() {
    this.dialogRef.close();
  }

  apply() {
    if (!this.canApply()) return;

    this.taskCommands.bulkUpdate(this.buildRequest());
    this.dialogRef.close();
  }

  protected controlId(key: BulkEditFieldKey): string {
    return `bulk-edit-${key}`;
  }

  protected priorityColor(priority: TaskPriority): string {
    return taskPriorityColors[priority];
  }

  protected removeLabelFor(label: string): string {
    return $localize`:Accessible label for the button that drops a field from a bulk edit. FIELD is the field's name:Remove ${label}:FIELD:`;
  }

  protected addField(key: BulkEditFieldKey) {
    this.seed(key);
    this.added.update((keys) => [...keys, key]);
  }

  protected removeField(key: BulkEditFieldKey) {
    this.added.update((keys) => keys.filter((added) => added !== key));
    this.seed(key);
  }

  protected toggleTag(name: string) {
    this.tags.update((tags) => toggleValue(tags, name));
  }

  protected toggleAssignee(userId: string) {
    this.assigneeIds.update((ids) => toggleValue(ids, userId));
  }

  protected toggleClearDueDate() {
    this.clearDueDate.update((clearing) => !clearing);
  }

  protected onRowsScroll(event: Event) {
    this.updateFades(event.target as HTMLElement);
  }

  private hintFor(key: BulkEditFieldKey): string {
    const tasks = this.tasks;

    switch (key) {
      case 'status':
        return statusHint(tasks, this.locale);
      case 'priority':
        return priorityHint(tasks, this.locale);
      case 'dueDate':
        return dueDateHint(tasks);
      case 'estimateType':
        return estimateTypeHint(tasks, this.locale);
      case 'estimateValue':
        return estimateValueHint(tasks);
      case 'tags':
        return tagsHint(tasks, this.tags().length, this.tagMode());
      case 'assignees':
        return assigneesHint(tasks, this.assigneeMode());
      case 'project':
        return projectHint(tasks, this.projectNames(), this.locale);
      case 'sprint':
        return sprintHint(tasks, this.sprintId() === NO_SPRINT, this.locale);
    }
  }

  private readonly assignedCounts = computed(() => {
    const counts = new Map<string, number>();

    for (const task of this.tasks) {
      for (const assignee of task.assignees) {
        counts.set(assignee.id, (counts.get(assignee.id) ?? 0) + 1);
      }
    }

    return counts;
  });

  private readonly taggedCounts = computed(() => {
    const counts = new Map<string, number>();

    for (const task of this.tasks) {
      for (const tag of task.tags) {
        counts.set(tag, (counts.get(tag) ?? 0) + 1);
      }
    }

    return counts;
  });

  private selectionHint(count: number): string {
    if (!count) return '';

    return $localize`:Says how many of the selected tasks already carry a tag or assignee. COUNT is how many do and TOTAL is how many are selected:on ${count}:COUNT: of ${this.taskCount}:TOTAL:`;
  }

  // Adding a field starts it on a usable value, and removing one puts it back so that adding it
  // again does not resurrect a choice the user has already dropped.
  private seed(key: BulkEditFieldKey) {
    switch (key) {
      case 'status':
        this.statusId.set(this.statuses.value()?.[0]?.id ?? null);
        break;
      case 'priority':
        this.priority.set(TaskPriority.none);
        break;
      case 'dueDate':
        this.dueDate.set('');
        this.clearDueDate.set(false);
        break;
      case 'estimateType':
        this.estimateType.set(EstimateType.storyPoints);
        break;
      case 'estimateValue':
        this.estimateValue.set(null);
        break;
      case 'tags':
        this.tags.set([]);
        this.tagMode.set(BulkCollectionMode.add);
        break;
      case 'assignees':
        this.assigneeIds.set([]);
        this.assigneeMode.set(BulkCollectionMode.add);
        break;
      case 'project':
        this.projectId.set(this.projects.value()?.[0]?.id ?? null);
        break;
      case 'sprint':
        this.sprintId.set(NO_SPRINT);
        break;
    }
  }

  private isIncomplete(key: BulkEditFieldKey): boolean {
    switch (key) {
      case 'status':
        return this.statusId() === null;
      case 'project':
        return this.projectId() === null;
      case 'sprint':
        return this.sprintId() === null;
      case 'estimateType':
        return this.estimateType() === null;
      case 'estimateValue':
        return this.estimateValue() === null;
      case 'dueDate':
        return !this.clearDueDate() && !this.dueDate();
      default:
        return false;
    }
  }

  private replacesExistingValue(key: BulkEditFieldKey): boolean {
    const tasks = this.tasks;

    switch (key) {
      case 'status':
        return tasks.some((task) => task.statusId !== this.statusId());
      case 'priority':
        return tasks.some((task) => {
          return task.priority !== null && task.priority !== this.priority();
        });
      case 'dueDate':
        return this.dueDateOverwrites();
      case 'estimateType':
        return tasks.some((task) => {
          return (
            task.estimateType !== null &&
            task.estimateType !== this.estimateType()
          );
        });
      case 'estimateValue':
        return tasks.some((task) => {
          return (
            task.estimateValue !== null &&
            task.estimateValue !== this.estimateValue()
          );
        });
      case 'tags':
        return this.collectionOverwrites(
          this.tagMode(),
          tasks.some((task) => task.tags.length > 0)
        );
      case 'assignees':
        return this.collectionOverwrites(
          this.assigneeMode(),
          tasks.some((task) => task.assignees.length > 0)
        );
      case 'project':
        return tasks.some((task) => task.projectId !== this.projectId());
      case 'sprint':
        return this.sprintOverwrites();
    }
  }

  private dueDateOverwrites(): boolean {
    const scheduled = this.tasks.filter((task) => !!task.dueDate);

    if (this.clearDueDate()) return scheduled.length > 0;

    const chosen = this.dueDate();

    return scheduled.some((task) => task.dueDate?.slice(0, 10) !== chosen);
  }

  private sprintOverwrites(): boolean {
    const chosen = this.sprintId();
    const inSprint = this.tasks.filter((task) => !!task.sprintId);

    if (chosen === NO_SPRINT) return inSprint.length > 0;

    return inSprint.some((task) => task.sprintId !== chosen);
  }

  private collectionOverwrites(
    mode: BulkCollectionMode,
    anyTaskHasValues: boolean
  ): boolean {
    return mode === BulkCollectionMode.replace && anyTaskHasValues;
  }

  private buildRequest(): BulkUpdateTasksRequest {
    const added = new Set(this.added());
    const request: BulkUpdateTasksRequest = {
      taskIds: this.tasks.map((task) => task.id),
    };

    if (added.has('status')) request.statusId = this.statusId();
    if (added.has('priority')) request.priority = this.priority();
    if (added.has('estimateType')) request.estimateType = this.estimateType();
    if (added.has('estimateValue'))
      request.estimateValue = this.estimateValue();
    if (added.has('project')) request.projectId = this.projectId();

    if (added.has('dueDate')) {
      if (this.clearDueDate()) {
        request.clearDueDate = true;
      } else {
        request.dueDate = this.dueDate();
      }
    }

    if (added.has('sprint')) {
      const sprintId = this.sprintId();

      if (sprintId === NO_SPRINT) {
        request.clearSprint = true;
      } else {
        request.sprintId = sprintId;
      }
    }

    if (added.has('tags')) {
      request.tags = this.tags();
      request.tagMode = this.tagMode();
    }

    if (added.has('assignees')) {
      request.assigneeIds = this.assigneeIds();
      request.assigneeMode = this.assigneeMode();
    }

    return request;
  }

  private measureRows() {
    const element = this.rows()?.nativeElement;

    if (!element) return;

    this.updateFades(element);
  }

  private updateFades(element: HTMLElement) {
    const remaining =
      element.scrollHeight - element.scrollTop - element.clientHeight;

    this.showTopFade.set(element.scrollTop > fadeEdgePx);
    this.showBottomFade.set(remaining > fadeEdgePx);
  }
}

function toggleValue(values: string[], value: string): string[] {
  const selected = values.includes(value);

  return selected
    ? values.filter((current) => current !== value)
    : [...values, value];
}
