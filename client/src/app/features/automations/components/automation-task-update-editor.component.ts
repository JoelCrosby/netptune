import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';
import {
  BulkCollectionMode,
  bulkCollectionModeOptions,
} from '@core/enums/bulk-collection-mode';
import { EstimateType, estimateTypeOptions } from '@core/enums/estimate-type';
import {
  TaskPriority,
  taskPriorityColors,
  taskPriorityOptions,
} from '@core/enums/task-priority';
import { AutomationBoardGroupOption } from '@core/models/automation-board-group-option';
import { WorkspaceAppUser } from '@core/models/appuser';
import { Status } from '@core/models/status';
import { Tag } from '@core/models/tag';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { toggleValue } from '@core/util/arrays';
import { LucideFlag } from '@lucide/angular';
import { AddFieldMenuComponent } from '@static/components/add-field-menu/add-field-menu.component';
import { InlineButtonComponent } from '@static/components/button/inline-button.component';
import {
  CollectionPickerComponent,
  CollectionPickerOption,
} from '@static/components/collection-picker/collection-picker.component';
import { DatePickerComponent } from '@static/components/date-picker/date-picker.component';
import { FormControlAppearanceDirective } from '@static/components/form-control/form-control.directives';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectGroupComponent } from '@static/components/form-select/form-select-group.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { NumberInputComponent } from '@static/components/number-input/number-input.component';
import { RemovableFieldRowComponent } from '@static/components/removable-field-row/removable-field-row.component';
import {
  AutomationDateUpdate,
  AutomationDateUpdateMode,
} from '@core/automations/automation.models';
import {
  TaskUpdateDraft,
  TaskUpdateFieldKey,
  addedTaskUpdateFields,
  taskUpdateFields,
} from '../models/task-update-fields';
import {
  boardGroupLabel,
  boardGroupSections,
} from '../models/board-group-sections';

type DateField = 'startDate' | 'dueDate';
type ClearableField = 'description' | 'owner' | 'estimate';

const NO_SPRINT = -1;

@Component({
  selector: 'app-automation-task-update-editor',
  imports: [
    AddFieldMenuComponent,
    CollectionPickerComponent,
    DatePickerComponent,
    FormControlAppearanceDirective,
    FormInputComponent,
    FormSelectComponent,
    FormSelectGroupComponent,
    FormSelectOptionComponent,
    InlineButtonComponent,
    LucideFlag,
    NgTemplateOutlet,
    NumberInputComponent,
    RemovableFieldRowComponent,
  ],
  host: { class: 'block' },
  template: `
    <div class="flex flex-col" appFormAppearance="quiet">
      <div class="flex flex-col gap-6.5">
        @for (field of addedFields(); track field.key) {
          <app-removable-field-row
            [label]="field.label"
            [controlId]="controlId(field.key)"
            [hint]="hints().get(field.key)"
            [removeLabel]="removeLabelFor(field.label)"
            (removed)="removeField(field.key)">
            @switch (field.key) {
              @case ('status') {
                <app-form-select
                  label=""
                  [noMargin]="true"
                  [name]="controlId('status')"
                  [value]="action().statusId ?? null"
                  (changed)="patch.emit({ statusId: $event })">
                  @for (status of statuses(); track status.id) {
                    <app-form-select-option [value]="status.id">
                      {{ status.name }}
                    </app-form-select-option>
                  }
                </app-form-select>
              }

              @case ('priority') {
                <app-form-select
                  label=""
                  [noMargin]="true"
                  [name]="controlId('priority')"
                  [value]="action().priority ?? null"
                  (changed)="patch.emit({ priority: $event })">
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

              @case ('name') {
                <app-form-input
                  i18n-placeholder="
                    Placeholder of the field that renames the task in an update
                    task action
                  "
                  placeholder="New task name"
                  [noMargin]="true"
                  [name]="controlId('name')"
                  [value]="action().taskName ?? ''"
                  (valueChange)="patch.emit({ taskName: $event })" />
              }

              @case ('description') {
                <div class="flex items-center gap-2">
                  <app-form-input
                    class="min-w-0 flex-1"
                    i18n-placeholder="
                      Placeholder of the field that rewrites the task
                      description in an update task action
                    "
                    placeholder="New description"
                    [noMargin]="true"
                    [name]="controlId('description')"
                    [disabled]="clearsDescription()"
                    [value]="action().taskDescription ?? ''"
                    (valueChange)="patch.emit({ taskDescription: $event })" />
                  <ng-container
                    *ngTemplateOutlet="
                      clearToggle;
                      context: {
                        field: 'description',
                        clearing: clearsDescription(),
                      }
                    " />
                </div>
              }

              @case ('owner') {
                <div class="flex items-center gap-2">
                  <app-form-select
                    class="min-w-0 flex-1"
                    label=""
                    i18n-placeholder="
                      Placeholder of the field that picks the task owner in an
                      update task action
                    "
                    placeholder="Choose a person"
                    [noMargin]="true"
                    [name]="controlId('owner')"
                    [disabled]="clearsOwner()"
                    [value]="action().ownerId ?? null"
                    (changed)="patch.emit({ ownerId: $event })">
                    @for (user of users(); track user.id) {
                      <app-form-select-option [value]="user.id">
                        {{ user.displayName }}
                      </app-form-select-option>
                    }
                  </app-form-select>
                  <ng-container
                    *ngTemplateOutlet="
                      clearToggle;
                      context: { field: 'owner', clearing: clearsOwner() }
                    " />
                </div>
              }

              @case ('assignees') {
                <app-collection-picker
                  [avatars]="true"
                  [options]="assigneeOptions()"
                  [selected]="action().assigneeIds ?? []"
                  [modeOptions]="collectionModeOptions"
                  [mode]="assigneeMode()"
                  [searchPlaceholder]="labels.searchPeople"
                  [listAriaLabel]="field.label"
                  [modeAriaLabel]="labels.assigneeModeLabel"
                  [emptyMessage]="labels.noPeople"
                  (toggled)="toggleAssignee($event)"
                  (cleared)="patch.emit({ assigneeIds: [] })"
                  (modeChange)="
                    patch.emit({
                      appendAssignees: $event === collectionMode.add,
                    })
                  " />
              }

              @case ('addTags') {
                <app-collection-picker
                  [options]="addTagOptions()"
                  [selected]="action().addTags ?? []"
                  [searchPlaceholder]="labels.searchTags"
                  [listAriaLabel]="field.label"
                  [emptyMessage]="labels.noTags"
                  (toggled)="toggleTag('addTags', $event)"
                  (cleared)="patch.emit({ addTags: [] })" />
              }

              @case ('removeTags') {
                <app-collection-picker
                  [options]="removeTagOptions()"
                  [selected]="action().removeTags ?? []"
                  [searchPlaceholder]="labels.searchTags"
                  [listAriaLabel]="field.label"
                  [emptyMessage]="labels.noTags"
                  (toggled)="toggleTag('removeTags', $event)"
                  (cleared)="patch.emit({ removeTags: [] })" />
              }

              @case ('startDate') {
                <ng-container
                  *ngTemplateOutlet="
                    dateEditor;
                    context: { field: 'startDate', label: field.label }
                  " />
              }

              @case ('dueDate') {
                <ng-container
                  *ngTemplateOutlet="
                    dateEditor;
                    context: { field: 'dueDate', label: field.label }
                  " />
              }

              @case ('estimate') {
                <div class="flex items-center gap-2">
                  <app-form-select
                    class="min-w-0 flex-1"
                    label=""
                    [noMargin]="true"
                    [name]="controlId('estimate')"
                    [disabled]="clearsEstimate()"
                    [value]="action().estimateType ?? estimateType.storyPoints"
                    (changed)="patch.emit({ estimateType: $event })">
                    @for (option of estimateOptions; track option.value) {
                      <app-form-select-option [value]="option.value">
                        {{ option.label }}
                      </app-form-select-option>
                    }
                  </app-form-select>
                  <app-number-input
                    class="w-38 shrink-0"
                    [min]="0"
                    [ariaLabel]="labels.estimateValue"
                    [disabled]="clearsEstimate()"
                    [value]="action().estimateValue ?? null"
                    (valueChange)="patch.emit({ estimateValue: $event })" />
                  <ng-container
                    *ngTemplateOutlet="
                      clearToggle;
                      context: { field: 'estimate', clearing: clearsEstimate() }
                    " />
                </div>
              }

              @case ('sprint') {
                <app-form-select
                  label=""
                  [noMargin]="true"
                  [name]="controlId('sprint')"
                  [value]="sprintValue()"
                  (changed)="setSprint($event)">
                  <app-form-select-option [value]="noSprint">
                    <span i18n="Option that clears the task's sprint">
                      No sprint
                    </span>
                  </app-form-select-option>
                  @for (sprint of sprints(); track sprint.id) {
                    <app-form-select-option [value]="sprint.id">
                      {{ sprint.projectName }} · {{ sprint.name }}
                    </app-form-select-option>
                  }
                </app-form-select>
              }

              @case ('boardGroup') {
                <app-form-select
                  label=""
                  [noMargin]="true"
                  [name]="controlId('boardGroup')"
                  [value]="action().boardGroupId ?? null"
                  (changed)="patch.emit({ boardGroupId: $event })">
                  @for (section of boardGroupSections(); track section.key) {
                    <app-form-select-group [label]="section.label">
                      @for (group of section.groups; track group.id) {
                        <app-form-select-option
                          [value]="group.id"
                          [selectedLabel]="boardGroupLabel(group)">
                          {{ group.name }}
                        </app-form-select-option>
                      }
                    </app-form-select-group>
                  }
                </app-form-select>
              }
            }
          </app-removable-field-row>
        } @empty {
          <p
            class="text-muted py-10 text-center text-sm"
            i18n="
              Shown in an update task action before any field has been added
            ">
            Nothing will change yet. Add a field below to start.
          </p>
        }
      </div>

      <app-add-field-menu
        class="mt-5"
        [options]="remainingFields()"
        (added)="addField($event)" />

      <ng-template #clearToggle let-field="field" let-clearing="clearing">
        <button
          type="button"
          app-inline-button
          appearance="soft"
          class="h-10 shrink-0 px-3"
          (click)="toggleClear(field)">
          @if (clearing) {
            <span
              i18n="
                Button that goes back to setting a value instead of clearing it
              ">
              Set a value instead
            </span>
          } @else {
            <span i18n="Button that clears a field instead of setting a value">
              Clear instead
            </span>
          }
        </button>
      </ng-template>

      <ng-template #dateEditor let-field="field" let-label="label">
        @let update = dateUpdate(field);
        <div class="flex items-center gap-2">
          <app-form-select
            class="w-56 shrink-0"
            label=""
            [noMargin]="true"
            [name]="controlId(field)"
            [value]="update?.mode ?? null"
            (changed)="setDateMode(field, $event)">
            <app-form-select-option [value]="dateMode.absolute">
              <span i18n="Action that sets a specific date">Set date</span>
            </app-form-select-option>
            <app-form-select-option [value]="dateMode.relativeDays">
              <span
                i18n="Date mode: a number of calendar days from the run date">
                Relative calendar days
              </span>
            </app-form-select-option>
            <app-form-select-option [value]="dateMode.relativeBusinessDays">
              <span
                i18n="Date mode: a number of working days from the run date">
                Relative business days
              </span>
            </app-form-select-option>
            <app-form-select-option [value]="dateMode.clear">
              <span i18n="Action that removes the date">Clear date</span>
            </app-form-select-option>
          </app-form-select>

          @if (update?.mode === dateMode.absolute) {
            <app-date-picker
              class="min-w-0 flex-1"
              [ariaLabel]="label"
              [value]="update?.date ?? ''"
              (valueChange)="setDateValue(field, { date: $event })" />
          } @else if (isRelative(update)) {
            <app-number-input
              class="w-38 shrink-0"
              [min]="-3650"
              [max]="3650"
              [ariaLabel]="labels.daysFromRunDate"
              [value]="update?.offset ?? null"
              (valueChange)="setDateValue(field, { offset: $event })" />
          }
        </div>
      </ng-template>
    </div>
  `,
})
export class AutomationTaskUpdateEditorComponent {
  protected readonly priorityOptions = taskPriorityOptions;
  protected readonly estimateType = EstimateType;
  protected readonly estimateOptions = estimateTypeOptions;
  protected readonly dateMode = AutomationDateUpdateMode;
  protected readonly collectionMode = BulkCollectionMode;
  protected readonly collectionModeOptions = bulkCollectionModeOptions;
  protected readonly noSprint = NO_SPRINT;

  protected readonly labels = {
    searchTags: $localize`:Placeholder in the box that searches tags:Search tags`,
    searchPeople: $localize`:Placeholder in the box that searches people:Search people`,
    assigneeModeLabel: $localize`:Accessible label for the control that chooses whether an edit adds assignees or replaces them:Assignee change mode`,
    noTags: $localize`:Shown when a workspace has no tags to pick from:No tags`,
    noPeople: $localize`:Shown when a workspace has no people to pick from:No people`,
    estimateValue: $localize`:Accessible label for the number an update task action sets the task estimate to:Estimate value`,
    daysFromRunDate: $localize`:Label of the days from run date field:Days from run date`,
  };

  private readonly relativeDateHint = $localize`:Explains how to set a date before the run date:Use a negative number for a date before the run date.`;

  readonly action = input.required<TaskUpdateDraft>();
  readonly statuses = input.required<Status[]>();
  readonly users = input.required<WorkspaceAppUser[]>();
  readonly tags = input.required<Tag[]>();
  readonly sprints = input.required<SprintViewModel[]>();
  readonly boardGroups = input.required<AutomationBoardGroupOption[]>();
  readonly defaultStatusId = input<number | null>(null);

  readonly patch = output<Partial<TaskUpdateDraft>>();

  protected readonly boardGroupSections = computed(() => {
    return boardGroupSections(this.boardGroups());
  });

  protected readonly boardGroupLabel = boardGroupLabel;

  private readonly added = computed(() => {
    return addedTaskUpdateFields(this.action());
  });

  protected readonly addedFields = computed(() => {
    const added = this.added();

    return taskUpdateFields.filter((field) => added.includes(field.key));
  });

  protected readonly remainingFields = computed(() => {
    const added = this.added();

    return taskUpdateFields.filter((field) => !added.includes(field.key));
  });

  protected readonly clearsDescription = computed(() => {
    return this.action().clearDescription === true;
  });

  protected readonly clearsOwner = computed(() => {
    return this.action().clearOwner === true;
  });

  protected readonly clearsEstimate = computed(() => {
    return this.action().clearEstimate === true;
  });

  protected readonly assigneeMode = computed(() => {
    return this.action().appendAssignees
      ? BulkCollectionMode.add
      : BulkCollectionMode.replace;
  });

  protected readonly sprintValue = computed(() => {
    const action = this.action();

    return action.clearSprint ? NO_SPRINT : (action.sprintId ?? null);
  });

  protected readonly assigneeOptions = computed<CollectionPickerOption[]>(
    () => {
      return this.users()
        .filter((user) => !user.isPending)
        .map((user) => {
          return {
            value: user.id,
            label: user.displayName,
            pictureUrl: user.pictureUrl,
            isServiceAccount: user.isServiceAccount,
          };
        });
    }
  );

  protected readonly addTagOptions = computed(() => {
    return this.tagOptionsExcluding(this.action().removeTags ?? []);
  });

  protected readonly removeTagOptions = computed(() => {
    return this.tagOptionsExcluding(this.action().addTags ?? []);
  });

  protected readonly hints = computed(() => {
    const hints = new Map<TaskUpdateFieldKey, string>();
    const action = this.action();

    if (action.assigneeIds) {
      hints.set('assignees', this.assigneesHint());
    }

    for (const field of ['startDate', 'dueDate'] as const) {
      const isRelativeDate = this.isRelative(action[field]);

      if (isRelativeDate) {
        hints.set(field, this.relativeDateHint);
      }
    }

    return hints;
  });

  protected controlId(key: TaskUpdateFieldKey): string {
    return `task-update-${key}`;
  }

  protected priorityColor(priority: TaskPriority): string {
    return taskPriorityColors[priority];
  }

  protected removeLabelFor(label: string): string {
    return $localize`:Accessible label for the button that drops a field from an edit. FIELD is the field's name:Remove ${label}:FIELD:`;
  }

  protected addField(key: TaskUpdateFieldKey) {
    this.patch.emit({
      ...this.seed(key),
      taskUpdateFields: [...this.added(), key],
    });
  }

  protected removeField(key: TaskUpdateFieldKey) {
    this.patch.emit({
      ...this.reset(key),
      taskUpdateFields: this.added().filter((added) => added !== key),
    });
  }

  protected toggleAssignee(userId: string) {
    const assigneeIds = this.action().assigneeIds ?? [];

    this.patch.emit({ assigneeIds: toggleValue(assigneeIds, userId) });
  }

  protected toggleTag(field: 'addTags' | 'removeTags', name: string) {
    const tags = this.action()[field] ?? [];

    this.patch.emit({ [field]: toggleValue(tags, name) });
  }

  protected toggleClear(field: ClearableField) {
    const action = this.action();

    switch (field) {
      case 'description':
        this.patch.emit({
          clearDescription: !action.clearDescription,
          taskDescription: action.clearDescription ? '' : null,
        });
        break;
      case 'owner':
        this.patch.emit({ clearOwner: !action.clearOwner, ownerId: null });
        break;
      case 'estimate':
        this.patch.emit({
          clearEstimate: !action.clearEstimate,
          estimateType: action.clearEstimate ? EstimateType.storyPoints : null,
          estimateValue: null,
        });
        break;
    }
  }

  protected setSprint(sprintId: number | null) {
    const clearSprint = sprintId === NO_SPRINT;

    this.patch.emit({
      clearSprint,
      sprintId: clearSprint ? null : sprintId,
    });
  }

  protected dateUpdate(field: DateField): AutomationDateUpdate | null {
    return this.action()[field] ?? null;
  }

  protected isRelative(update: AutomationDateUpdate | null | undefined) {
    return (
      update?.mode === AutomationDateUpdateMode.relativeDays ||
      update?.mode === AutomationDateUpdateMode.relativeBusinessDays
    );
  }

  protected setDateMode(field: DateField, mode: AutomationDateUpdateMode) {
    this.patch.emit({
      [field]: {
        mode,
        date: mode === AutomationDateUpdateMode.absolute ? '' : null,
        offset: this.isRelative({ mode }) ? 0 : null,
      },
    });
  }

  protected setDateValue(
    field: DateField,
    value: Partial<AutomationDateUpdate>
  ) {
    const update = this.dateUpdate(field);

    if (!update) return;

    this.patch.emit({ [field]: { ...update, ...value } });
  }

  private assigneesHint(): string {
    if (this.action().appendAssignees) {
      return $localize`:Says that an update task action keeps the task's current assignees:Adds these people alongside the task's current assignees.`;
    }

    return $localize`:Says that an update task action swaps out the task's assignees:Replaces the task's assignees. Leave empty to unassign everyone.`;
  }

  private tagOptionsExcluding(names: string[]): CollectionPickerOption[] {
    const excluded = new Set(names);

    return this.tags()
      .filter((tag) => !excluded.has(tag.name))
      .map((tag) => ({ value: tag.name, label: tag.name }));
  }

  private seed(key: TaskUpdateFieldKey): Partial<TaskUpdateDraft> {
    switch (key) {
      case 'status':
        return {
          statusId: this.defaultStatusId() ?? this.statuses()[0]?.id ?? null,
        };
      case 'priority':
        return { priority: TaskPriority.none };
      case 'name':
        return { taskName: '' };
      case 'description':
        return { taskDescription: '', clearDescription: false };
      case 'owner':
        return { ownerId: null, clearOwner: false };
      case 'assignees':
        return { assigneeIds: [], appendAssignees: true };
      case 'addTags':
        return { addTags: [] };
      case 'removeTags':
        return { removeTags: [] };
      case 'startDate':
      case 'dueDate':
        return {
          [key]: { mode: AutomationDateUpdateMode.absolute, date: '' },
        };
      case 'estimate':
        return {
          estimateType: EstimateType.storyPoints,
          estimateValue: null,
          clearEstimate: false,
        };
      case 'sprint':
        return { sprintId: null, clearSprint: true };
      case 'boardGroup':
        return { boardGroupId: this.boardGroups()[0]?.id ?? null };
    }
  }

  private reset(key: TaskUpdateFieldKey): Partial<TaskUpdateDraft> {
    switch (key) {
      case 'status':
        return { statusId: null };
      case 'priority':
        return { priority: null };
      case 'name':
        return { taskName: null };
      case 'description':
        return { taskDescription: null, clearDescription: false };
      case 'owner':
        return { ownerId: null, clearOwner: false };
      case 'assignees':
        return { assigneeIds: null, appendAssignees: false };
      case 'addTags':
        return { addTags: [] };
      case 'removeTags':
        return { removeTags: [] };
      case 'startDate':
      case 'dueDate':
        return { [key]: null };
      case 'estimate':
        return {
          estimateType: null,
          estimateValue: null,
          clearEstimate: false,
        };
      case 'sprint':
        return { sprintId: null, clearSprint: false };
      case 'boardGroup':
        return { boardGroupId: null };
    }
  }
}
