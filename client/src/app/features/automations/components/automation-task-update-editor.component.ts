import { Component, computed, input, output } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { EstimateType, estimateTypeOptions } from '@core/enums/estimate-type';
import { TaskPriority, taskPriorityOptions } from '@core/enums/task-priority';
import { AutomationBoardGroupOption } from '@core/models/automation-board-group-option';
import { WorkspaceAppUser } from '@core/models/appuser';
import { Status } from '@core/models/status';
import { Tag } from '@core/models/tag';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectTagsOptionComponent } from '@static/components/form-select-tags/form-select-tags-option.component';
import { FormSelectTagsComponent } from '@static/components/form-select-tags/form-select-tags.component';
import {
  AutomationAction,
  AutomationDateUpdate,
  AutomationDateUpdateMode,
} from '../models/automation.models';

@Component({
  selector: 'app-automation-task-update-editor',
  imports: [
    CheckboxComponent,
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    FormSelectTagsComponent,
    FormSelectTagsOptionComponent,
    NgTemplateOutlet,
  ],
  template: `
    <div class="flex flex-col gap-4">
      <div class="flex flex-col gap-3">
        <app-checkbox
          [checked]="hasStatusUpdate()"
          (changed)="
            patch.emit({
              statusId: $event ? defaultStatusId() : null,
            })
          ">
          <span i18n="Action that changes the task status">Set status</span>
        </app-checkbox>
        @if (hasStatusUpdate()) {
          <app-form-select
            i18n-label="Label of the status field"
            label="Status"
            [noMargin]="true"
            [value]="action().statusId ?? null"
            (changed)="patch.emit({ statusId: $event })">
            @for (status of statuses(); track status.id) {
              <app-form-select-option [value]="status.id">
                {{ status.name }}
              </app-form-select-option>
            }
          </app-form-select>
        }
      </div>

      <div class="flex flex-col gap-3">
        <app-checkbox
          [checked]="hasPriorityUpdate()"
          (changed)="
            patch.emit({
              priority: $event ? defaultTaskPriority : null,
            })
          ">
          <span i18n="Action that changes the task priority">Set priority</span>
        </app-checkbox>
        @if (hasPriorityUpdate()) {
          <app-form-select
            i18n-label="Label of the priority field"
            label="Priority"
            [noMargin]="true"
            [value]="action().priority ?? null"
            (changed)="patch.emit({ priority: $event })">
            @for (priority of taskPriorities; track priority.value) {
              <app-form-select-option [value]="priority.value">
                {{ priority.label }}
              </app-form-select-option>
            }
          </app-form-select>
        }
      </div>

      <app-form-input
        i18n-label="Label of the set name field"
        label="Set name"
        i18n-placeholder="
          Placeholder text: Leave empty to keep the current name
        "
        placeholder="Leave empty to keep the current name"
        [noMargin]="true"
        [value]="action().taskName ?? ''"
        (valueChange)="patch.emit({ taskName: $event || null })" />

      <div class="flex flex-col gap-2">
        <app-form-input
          i18n-label="Label of the set description field"
          label="Set description"
          i18n-placeholder="
            Placeholder text: Leave empty to keep the current description
          "
          placeholder="Leave empty to keep the current description"
          [noMargin]="true"
          [disabled]="shouldClearDescription()"
          [value]="action().taskDescription ?? ''"
          (valueChange)="patch.emit({ taskDescription: $event || null })" />
        <app-checkbox
          [checked]="shouldClearDescription()"
          (changed)="
            patch.emit({
              clearDescription: $event,
              taskDescription: $event ? null : action().taskDescription,
            })
          ">
          <span i18n="Action that empties the task description">
            Clear description
          </span>
        </app-checkbox>
      </div>

      <div class="flex flex-col gap-2">
        <app-form-select
          i18n-label="Label of the set owner field"
          label="Set owner"
          [noMargin]="true"
          [disabled]="shouldClearOwner()"
          [value]="action().ownerId ?? null"
          (changed)="patch.emit({ ownerId: $event })">
          <app-form-select-option [value]="null">
            <span i18n="Option that leaves the task owner unchanged">
              Keep current owner
            </span>
          </app-form-select-option>
          @for (user of users(); track user.id) {
            <app-form-select-option [value]="user.id">
              {{ user.displayName }}
            </app-form-select-option>
          }
        </app-form-select>
        <app-checkbox
          [checked]="shouldClearOwner()"
          (changed)="
            patch.emit({
              clearOwner: $event,
              ownerId: $event ? null : action().ownerId,
            })
          ">
          <span i18n="Action that removes the task owner">Clear owner</span>
        </app-checkbox>
      </div>

      <div class="flex flex-col gap-2">
        <app-checkbox
          [checked]="isReplacingAssignees()"
          (changed)="patch.emit({ assigneeIds: $event ? [] : null })">
          <span i18n="Action that replaces the task assignees">
            Replace assignees
          </span>
        </app-checkbox>
        @if (isReplacingAssignees()) {
          <app-form-select-tags
            i18n-label="Label of the assignees field"
            label="Assignees"
            i18n-placeholder="
              Placeholder text: Choose assignees; leave empty to unassign all
            "
            placeholder="Choose assignees; leave empty to unassign all"
            [value]="action().assigneeIds ?? []"
            (changed)="patch.emit({ assigneeIds: $event })">
            @for (user of users(); track user.id) {
              <app-form-select-tags-option [value]="user.id">
                {{ user.displayName }}
              </app-form-select-tags-option>
            }
          </app-form-select-tags>
        }
      </div>

      <app-form-select-tags
        i18n-label="Label of the add tags field"
        label="Add tags"
        i18n-placeholder="Placeholder text: Choose tags to add"
        placeholder="Choose tags to add"
        [value]="action().addTags ?? []"
        (changed)="patch.emit({ addTags: $event })">
        @for (tag of availableAddTags(); track tag.id) {
          <app-form-select-tags-option [value]="tag.name">
            {{ tag.name }}
          </app-form-select-tags-option>
        }
      </app-form-select-tags>

      <app-form-select-tags
        i18n-label="Label of the remove tags field"
        label="Remove tags"
        i18n-placeholder="Placeholder text: Choose tags to remove"
        placeholder="Choose tags to remove"
        [value]="action().removeTags ?? []"
        (changed)="patch.emit({ removeTags: $event })">
        @for (tag of availableRemoveTags(); track tag.id) {
          <app-form-select-tags-option [value]="tag.name">
            {{ tag.name }}
          </app-form-select-tags-option>
        }
      </app-form-select-tags>

      <ng-container
        *ngTemplateOutlet="
          dateEditor;
          context: {
            label: startDateLabel,
            value: action().startDate,
            field: 'startDate',
          }
        " />

      <ng-container
        *ngTemplateOutlet="
          dateEditor;
          context: {
            label: dueDateLabel,
            value: action().dueDate,
            field: 'dueDate',
          }
        " />

      <div class="flex flex-col gap-2">
        <app-checkbox
          [checked]="isEstimateUpdateEnabled()"
          (changed)="toggleEstimate($event)">
          <span i18n="Action that sets the task estimate">Set estimate</span>
        </app-checkbox>
        @if (isEstimateUpdateEnabled()) {
          <div class="flex flex-col gap-2">
            <app-form-select
              i18n-label="Label of the estimate type field"
              label="Estimate type"
              [noMargin]="true"
              [disabled]="shouldClearEstimate()"
              [value]="action().estimateType ?? estimateType.storyPoints"
              (changed)="patch.emit({ estimateType: $event })">
              @for (option of estimateTypes; track option.value) {
                <app-form-select-option [value]="option.value">
                  {{ option.label }}
                </app-form-select-option>
              }
            </app-form-select>
            <app-form-input
              i18n-label="Label of the value field"
              label="Value"
              type="number"
              min="0"
              [noMargin]="true"
              [disabled]="shouldClearEstimate()"
              [value]="numberValue(action().estimateValue)"
              (valueChange)="
                patch.emit({ estimateValue: parseNumber($event) })
              " />
          </div>
          <app-checkbox
            [checked]="shouldClearEstimate()"
            (changed)="
              patch.emit({
                clearEstimate: $event,
                estimateType: $event
                  ? null
                  : (action().estimateType ?? estimateType.storyPoints),
                estimateValue: $event ? null : action().estimateValue,
              })
            ">
            <span i18n="Action that removes the task estimate">
              Clear estimate
            </span>
          </app-checkbox>
        }
      </div>

      <div class="flex flex-col gap-2">
        <app-form-select
          i18n-label="Label of the move to sprint field"
          label="Move to sprint"
          [noMargin]="true"
          [disabled]="shouldMoveToBacklog()"
          [value]="action().sprintId ?? null"
          (changed)="patch.emit({ sprintId: $event })">
          <app-form-select-option [value]="null">
            <span i18n="Option that leaves the task sprint unchanged">
              Keep current sprint
            </span>
          </app-form-select-option>
          @for (sprint of sprints(); track sprint.id) {
            <app-form-select-option [value]="sprint.id">
              {{ sprint.projectName }} · {{ sprint.name }}
            </app-form-select-option>
          }
        </app-form-select>
        <app-checkbox
          [checked]="shouldMoveToBacklog()"
          (changed)="
            patch.emit({
              clearSprint: $event,
              sprintId: $event ? null : action().sprintId,
            })
          ">
          <span i18n="Action that removes the task from its sprint">
            Move to backlog
          </span>
        </app-checkbox>
      </div>

      <app-form-select
        i18n-label="Label of the move to board group field"
        label="Move to board group"
        [noMargin]="true"
        [value]="action().boardGroupId ?? null"
        (changed)="patch.emit({ boardGroupId: $event })">
        <app-form-select-option [value]="null">
          <span i18n="Option that leaves the board group unchanged">
            Keep current board group
          </span>
        </app-form-select-option>
        @for (group of boardGroups(); track group.id) {
          <app-form-select-option [value]="group.id">
            {{ group.projectName }} · {{ group.boardName }} · {{ group.name }}
          </app-form-select-option>
        }
      </app-form-select>
    </div>

    <ng-template
      #dateEditor
      let-label="label"
      let-value="value"
      let-field="field">
      @let selectedDateMode = value?.mode;
      <div class="flex flex-col gap-2">
        <app-form-select
          [label]="label"
          [noMargin]="true"
          [value]="selectedDateMode ?? null"
          (changed)="setDateMode(field, $event)">
          <app-form-select-option [value]="null">
            <span i18n="Option that leaves the date unchanged">
              Keep current date
            </span>
          </app-form-select-option>
          <app-form-select-option [value]="dateMode.absolute">
            <span i18n="Action that sets a specific date">Set date</span>
          </app-form-select-option>
          <app-form-select-option [value]="dateMode.relativeDays">
            <span i18n="Date mode: a number of calendar days from the run date">
              Relative calendar days
            </span>
          </app-form-select-option>
          <app-form-select-option [value]="dateMode.relativeBusinessDays">
            <span i18n="Date mode: a number of working days from the run date">
              Relative business days
            </span>
          </app-form-select-option>
          <app-form-select-option [value]="dateMode.clear">
            <span i18n="Action that removes the date">Clear date</span>
          </app-form-select-option>
        </app-form-select>

        @if (selectedDateMode === dateMode.absolute) {
          <app-form-input
            type="date"
            [label]="label"
            [noMargin]="true"
            [value]="value?.date ?? ''"
            (valueChange)="setAbsoluteDate(field, value, $event)" />
        } @else if (
          selectedDateMode === dateMode.relativeDays ||
          selectedDateMode === dateMode.relativeBusinessDays
        ) {
          <app-form-input
            type="number"
            min="-3650"
            max="3650"
            i18n-label="Label of the days from run date field"
            label="Days from run date"
            i18n-hint="Explains how to set a date before the run date"
            hint="Use a negative number for a date before the run date."
            [noMargin]="true"
            [value]="numberValue(value?.offset)"
            (valueChange)="setDateOffset(field, value, $event)" />
        }
      </div>
    </ng-template>
  `,
})
export class AutomationTaskUpdateEditorComponent {
  /** $localize is a TS tag and cannot appear inside a component template. */
  protected readonly startDateLabel = $localize`:Label of the task start date field:Start date`;
  protected readonly dueDateLabel = $localize`:Label of the task due date field:Due date`;

  readonly defaultTaskPriority = TaskPriority.none;
  readonly taskPriorities = taskPriorityOptions;
  readonly estimateType = EstimateType;
  readonly estimateTypes = estimateTypeOptions;
  readonly dateMode = AutomationDateUpdateMode;

  readonly action = input.required<AutomationAction>();
  readonly statuses = input.required<Status[]>();
  readonly users = input.required<WorkspaceAppUser[]>();
  readonly tags = input.required<Tag[]>();
  readonly sprints = input.required<SprintViewModel[]>();
  readonly boardGroups = input.required<AutomationBoardGroupOption[]>();
  readonly defaultStatusId = input<number | null>(null);
  readonly patch = output<Partial<AutomationAction>>();

  readonly hasStatusUpdate = computed(() =>
    this.hasValue(this.action().statusId)
  );
  readonly hasPriorityUpdate = computed(() =>
    this.hasValue(this.action().priority)
  );
  readonly shouldClearDescription = computed(
    () => this.action().clearDescription === true
  );
  readonly shouldClearOwner = computed(() => this.action().clearOwner === true);
  readonly isReplacingAssignees = computed(
    () => this.action().assigneeIds !== null
  );
  readonly shouldClearEstimate = computed(
    () => this.action().clearEstimate === true
  );
  readonly isEstimateUpdateEnabled = computed(() => {
    const action = this.action();

    return (
      this.hasValue(action.estimateType) ||
      this.hasValue(action.estimateValue) ||
      action.clearEstimate === true
    );
  });
  readonly shouldMoveToBacklog = computed(
    () => this.action().clearSprint === true
  );

  hasValue<T>(value: T | null | undefined): value is T {
    return value !== null && value !== undefined;
  }

  toggleEstimate(enabled: boolean) {
    this.patch.emit({
      estimateType: enabled ? EstimateType.storyPoints : null,
      estimateValue: enabled ? 0 : null,
      clearEstimate: false,
    });
  }

  availableAddTags(): Tag[] {
    const removed = new Set(this.action().removeTags ?? []);

    return this.tags().filter((tag) => !removed.has(tag.name));
  }

  availableRemoveTags(): Tag[] {
    const added = new Set(this.action().addTags ?? []);

    return this.tags().filter((tag) => !added.has(tag.name));
  }

  setDateMode(
    field: 'startDate' | 'dueDate',
    mode: AutomationDateUpdateMode | null
  ) {
    const update =
      mode === null
        ? null
        : {
            mode,
            date: mode === AutomationDateUpdateMode.absolute ? '' : null,
            offset:
              mode === AutomationDateUpdateMode.relativeDays ||
              mode === AutomationDateUpdateMode.relativeBusinessDays
                ? 0
                : null,
          };

    this.patch.emit({ [field]: update });
  }

  setAbsoluteDate(
    field: 'startDate' | 'dueDate',
    update: AutomationDateUpdate,
    date: string
  ) {
    this.patch.emit({ [field]: { ...update, date } });
  }

  setDateOffset(
    field: 'startDate' | 'dueDate',
    update: AutomationDateUpdate,
    value: string
  ) {
    this.patch.emit({
      [field]: { ...update, offset: this.parseInteger(value) },
    });
  }

  parseInteger(value: string): number | null {
    const number = Number(value);

    return Number.isInteger(number) && value.trim() ? number : null;
  }

  parseNumber(value: string): number | null {
    const number = Number(value);

    return Number.isFinite(number) && value.trim() ? number : null;
  }

  numberValue(value: number | null | undefined): string {
    return value === null || value === undefined ? '' : String(value);
  }
}
