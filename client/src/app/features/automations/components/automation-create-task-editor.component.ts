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
        label="Task name"
        placeholder="Follow up on {{ '{{task.key}}' }}"
        [required]="true"
        [noMargin]="true"
        [hint]="variableHint"
        [value]="action().taskName ?? ''"
        (valueChange)="patch.emit({ taskName: $event })" />

      <app-form-textarea
        label="Description"
        rows="3"
        [noMargin]="true"
        [value]="action().taskDescription ?? ''"
        (valueChange)="patch.emit({ taskDescription: $event })" />

      <div class="grid gap-3 md:grid-cols-2">
        <app-form-select
          label="Status"
          [noMargin]="true"
          [value]="action().statusId ?? null"
          (changed)="patch.emit({ statusId: $event })">
          <app-form-select-option [value]="null">
            Project default
          </app-form-select-option>
          @for (status of statuses(); track status.id) {
            <app-form-select-option [value]="status.id">
              {{ status.name }}
            </app-form-select-option>
          }
        </app-form-select>

        <app-form-select
          label="Priority"
          [noMargin]="true"
          [value]="action().priority ?? null"
          (changed)="patch.emit({ priority: $event })">
          <app-form-select-option [value]="null">None</app-form-select-option>
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
          Copy assignees from the triggering task
        </app-checkbox>
        @if (!copiesAssignees()) {
          <app-form-select-tags
            label="Assignees"
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
        label="Tags"
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
          label="Due date"
          [noMargin]="true"
          [value]="dueDateMode()"
          (changed)="setDueDateMode($event)">
          <app-form-select-option [value]="null">
            No due date
          </app-form-select-option>
          <app-form-select-option [value]="dateMode.relativeDays">
            Days after creation
          </app-form-select-option>
          <app-form-select-option [value]="dateMode.relativeBusinessDays">
            Business days after creation
          </app-form-select-option>
          <app-form-select-option [value]="dateMode.absolute">
            On a fixed date
          </app-form-select-option>
        </app-form-select>

        @if (usesDueDateOffset()) {
          <app-form-input
            label="Days"
            type="number"
            [noMargin]="true"
            [value]="dueDateOffset()"
            (valueChange)="setDueDateOffset($event)" />
        } @else if (usesDueDateValue()) {
          <app-form-input
            label="Date"
            type="date"
            [noMargin]="true"
            [value]="action().dueDate?.date ?? ''"
            (valueChange)="setDueDate($event)" />
        }
      </div>

      <div class="grid gap-3 md:grid-cols-2">
        <app-form-select
          label="Sprint"
          [noMargin]="true"
          [value]="action().sprintId ?? null"
          (changed)="patch.emit({ sprintId: $event })">
          <app-form-select-option [value]="null"
            >Backlog</app-form-select-option
          >
          @for (sprint of sprints(); track sprint.id) {
            <app-form-select-option [value]="sprint.id">
              {{ sprint.name }}
            </app-form-select-option>
          }
        </app-form-select>

        <app-form-select
          label="Board group"
          [noMargin]="true"
          [value]="action().boardGroupId ?? null"
          (changed)="patch.emit({ boardGroupId: $event })">
          <app-form-select-option [value]="null">
            Project default
          </app-form-select-option>
          @for (boardGroup of boardGroups(); track boardGroup.id) {
            <app-form-select-option [value]="boardGroup.id">
              {{ boardGroup.name }}
            </app-form-select-option>
          }
        </app-form-select>
      </div>

      <app-form-select
        label="Link to the triggering task"
        [noMargin]="true"
        [value]="action().linkRelationTypeId ?? null"
        (changed)="patch.emit({ linkRelationTypeId: $event })">
        <app-form-select-option [value]="null">
          Do not link
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
