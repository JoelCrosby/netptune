import { Component, computed, input, output } from '@angular/core';
import { taskPriorityOptions } from '@core/enums/task-priority';
import { AutomationBoardGroupOption } from '@core/models/automation-board-group-option';
import { WorkspaceAppUser } from '@core/models/appuser';
import { RelationType } from '@core/models/relation-type';
import { Status } from '@core/models/status';
import { Tag } from '@core/models/tag';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectTagsOptionComponent } from '@static/components/form-select-tags/form-select-tags-option.component';
import { FormSelectTagsComponent } from '@static/components/form-select-tags/form-select-tags.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { messageVariables } from '../models/automation-copy';
import {
  AutomationAction,
  AutomationDateUpdate,
  AutomationDateUpdateMode,
} from '../models/automation.models';

@Component({
  selector: 'app-automation-create-task-editor',
  imports: [
    CheckboxComponent,
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    FormSelectTagsComponent,
    FormSelectTagsOptionComponent,
    FormTextAreaComponent,
  ],
  template: `
    <div class="flex flex-col gap-4">
      <app-form-input
        i18n-label="Label of the task name field"
        label="Task name"
        placeholder="Follow up on {{ '{{task.key}}' }}"
        [required]="true"
        [noMargin]="true"
        [hint]="variableHint"
        [value]="action().taskName ?? ''"
        (valueChange)="patch.emit({ taskName: $event })" />

      <app-form-textarea
        i18n-label="Label of the description field"
        label="Description"
        rows="3"
        [noMargin]="true"
        [value]="action().taskDescription ?? ''"
        (valueChange)="patch.emit({ taskDescription: $event })" />

      <div class="grid gap-3 md:grid-cols-2">
        <app-form-select
          i18n-label="Label of the status field"
          label="Status"
          [noMargin]="true"
          [value]="action().statusId ?? null"
          (changed)="patch.emit({ statusId: $event })">
          <app-form-select-option [value]="null">
            <span i18n="Option that uses the project default">
              Project default
            </span>
          </app-form-select-option>
          @for (status of statuses(); track status.id) {
            <app-form-select-option [value]="status.id">
              {{ status.name }}
            </app-form-select-option>
          }
        </app-form-select>

        <app-form-select
          i18n-label="Label of the priority field"
          label="Priority"
          [noMargin]="true"
          [value]="action().priority ?? null"
          (changed)="patch.emit({ priority: $event })">
          <app-form-select-option
            [value]="null"
            i18n="Shown in place of an empty value">
            None
          </app-form-select-option>
          @for (priority of taskPriorities; track priority.value) {
            <app-form-select-option [value]="priority.value">
              {{ priority.label }}
            </app-form-select-option>
          }
        </app-form-select>
      </div>

      <div class="flex flex-col gap-2">
        <app-checkbox
          [checked]="copiesAssignees()"
          (changed)="setCopyAssignees($event)">
          <span
            i18n="
              Option that copies assignees from the task that triggered the rule
            ">
            Copy assignees from the triggering task
          </span>
        </app-checkbox>
        @if (!copiesAssignees()) {
          <app-form-select-tags
            i18n-label="Label of the assignees field"
            label="Assignees"
            i18n-placeholder="
              Placeholder text: Choose assignees; leave empty to unassign
            "
            placeholder="Choose assignees; leave empty to unassign"
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
        i18n-label="Label of the tags field"
        label="Tags"
        i18n-placeholder="Placeholder text: Choose tags to add"
        placeholder="Choose tags to add"
        [value]="action().addTags ?? []"
        (changed)="patch.emit({ addTags: $event })">
        @for (tag of tags(); track tag.id) {
          <app-form-select-tags-option [value]="tag.name">
            {{ tag.name }}
          </app-form-select-tags-option>
        }
      </app-form-select-tags>

      <div class="grid gap-3 md:grid-cols-2">
        <app-form-select
          i18n-label="Label of the due date field"
          label="Due date"
          [noMargin]="true"
          [value]="dueDateMode()"
          (changed)="setDueDateMode($event)">
          <app-form-select-option [value]="null">
            <span i18n="Option that leaves the created task without a due date">
              No due date
            </span>
          </app-form-select-option>
          <app-form-select-option [value]="dateMode.relativeDays">
            <span
              i18n="Due date mode: a number of calendar days after creation">
              Days after creation
            </span>
          </app-form-select-option>
          <app-form-select-option [value]="dateMode.relativeBusinessDays">
            <span i18n="Due date mode: a number of working days after creation">
              Business days after creation
            </span>
          </app-form-select-option>
          <app-form-select-option [value]="dateMode.absolute">
            <span i18n="Due date mode: a specific date">On a fixed date</span>
          </app-form-select-option>
        </app-form-select>

        @if (usesDueDateOffset()) {
          <app-form-input
            i18n-label="Label of the days field"
            label="Days"
            type="number"
            [noMargin]="true"
            [value]="dueDateOffset()"
            (valueChange)="setDueDateOffset($event)" />
        } @else if (usesDueDateValue()) {
          <app-form-input
            i18n-label="Label of the date field"
            label="Date"
            type="date"
            [noMargin]="true"
            [value]="action().dueDate?.date ?? ''"
            (valueChange)="setDueDate($event)" />
        }
      </div>

      <div class="grid gap-3 md:grid-cols-2">
        <app-form-select
          i18n-label="Label of the sprint field"
          label="Sprint"
          [noMargin]="true"
          [value]="action().sprintId ?? null"
          (changed)="patch.emit({ sprintId: $event })">
          <app-form-select-option [value]="null">
            <span i18n="Option placing the created task outside any sprint">
              Backlog
            </span>
          </app-form-select-option>
          @for (sprint of sprints(); track sprint.id) {
            <app-form-select-option [value]="sprint.id">
              {{ sprint.name }}
            </app-form-select-option>
          }
        </app-form-select>

        <app-form-select
          i18n-label="Label of the board group field"
          label="Board group"
          [noMargin]="true"
          [value]="action().boardGroupId ?? null"
          (changed)="patch.emit({ boardGroupId: $event })">
          <app-form-select-option [value]="null">
            <span i18n="Option that uses the project default">
              Project default
            </span>
          </app-form-select-option>
          @for (boardGroup of boardGroups(); track boardGroup.id) {
            <app-form-select-option [value]="boardGroup.id">
              {{ boardGroup.name }}
            </app-form-select-option>
          }
        </app-form-select>
      </div>

      <app-form-select
        i18n-label="Label of the option that links a notification to its task"
        label="Link to the triggering task"
        [noMargin]="true"
        [value]="action().linkRelationTypeId ?? null"
        (changed)="patch.emit({ linkRelationTypeId: $event })">
        <app-form-select-option [value]="null">
          <span i18n="Option that creates the task without linking it">
            Do not link
          </span>
        </app-form-select-option>
        @for (relationType of relationTypes(); track relationType.id) {
          <app-form-select-option [value]="relationType.id">
            {{ relationType.name }}
          </app-form-select-option>
        }
      </app-form-select>
    </div>
  `,
})
export class AutomationCreateTaskEditorComponent {
  taskPriorities = taskPriorityOptions;
  dateMode = AutomationDateUpdateMode;
  variableHint = `Variables: ${messageVariables
    .map((variable) => `{{${variable}}}`)
    .join(' ')}`;

  action = input.required<AutomationAction>();
  statuses = input.required<Status[]>();
  users = input.required<WorkspaceAppUser[]>();
  tags = input.required<Tag[]>();
  sprints = input.required<SprintViewModel[]>();
  boardGroups = input.required<AutomationBoardGroupOption[]>();
  relationTypes = input.required<RelationType[]>();
  patch = output<Partial<AutomationAction>>();

  copiesAssignees = computed(() => this.action().copyAssignees === true);
  dueDateMode = computed(() => this.action().dueDate?.mode ?? null);
  usesDueDateOffset = computed(() => {
    const mode = this.dueDateMode();

    return (
      mode === AutomationDateUpdateMode.relativeDays ||
      mode === AutomationDateUpdateMode.relativeBusinessDays
    );
  });
  usesDueDateValue = computed(() => {
    return this.dueDateMode() === AutomationDateUpdateMode.absolute;
  });

  dueDateOffset = computed(() => String(this.action().dueDate?.offset ?? 0));

  setCopyAssignees(copyAssignees: boolean) {
    this.patch.emit({
      copyAssignees,
      assigneeIds: copyAssignees ? [] : (this.action().assigneeIds ?? []),
    });
  }

  setDueDateMode(mode: AutomationDateUpdateMode | null) {
    if (mode === null) {
      this.patch.emit({ dueDate: null });

      return;
    }

    const isAbsolute = mode === AutomationDateUpdateMode.absolute;
    const dueDate: AutomationDateUpdate = {
      mode,
      date: null,
      offset: isAbsolute ? null : 0,
    };

    this.patch.emit({ dueDate });
  }

  setDueDateOffset(value: string) {
    const offset = Number.parseInt(value, 10);
    const dueDate = this.action().dueDate;

    if (!dueDate) return;

    this.patch.emit({
      dueDate: {
        ...dueDate,
        offset: Number.isNaN(offset) ? 0 : offset,
      },
    });
  }

  setDueDate(value: string) {
    const dueDate = this.action().dueDate;

    if (!dueDate) return;

    this.patch.emit({
      dueDate: {
        ...dueDate,
        date: value || null,
      },
    });
  }
}
