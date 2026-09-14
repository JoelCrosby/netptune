import { Component, computed, inject, input, signal } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { LucideChevronDown } from '@lucide/angular';
import { cn } from '@static/components/button/button.variants';
import {
  fieldLabelClass,
  fieldRowClass,
} from '@static/components/field-row/field-row.component';
import { TaskAssigneeFieldComponent } from '../parts/fields/task-assignee-field.component';
import { TaskDateFieldComponent } from '../parts/fields/task-date-field.component';
import { TaskEstimateFieldComponent } from '../parts/fields/task-estimate-field.component';
import { TaskPriorityFieldComponent } from '../parts/fields/task-priority-field.component';
import { TaskProjectFieldComponent } from '../parts/fields/task-project-field.component';
import { TaskReporterFieldComponent } from '../parts/fields/task-reporter-field.component';
import { TaskSprintFieldComponent } from '../parts/fields/task-sprint-field.component';
import { TaskStatusPickerComponent } from '../pickers/task-status-picker.component';
import { TaskDetailService } from '../task-detail.service';

export type TaskDetailField =
  | 'status'
  | 'assignee'
  | 'reporter'
  | 'priority'
  | 'estimate'
  | 'startDate'
  | 'dueDate'
  | 'project'
  | 'sprint';

const ALL_FIELDS: TaskDetailField[] = [
  'status',
  'assignee',
  'reporter',
  'priority',
  'estimate',
  'startDate',
  'dueDate',
  'project',
  'sprint',
];

@Component({
  selector: 'app-task-detail-field-rows',
  imports: [
    LucideChevronDown,
    TaskAssigneeFieldComponent,
    TaskDateFieldComponent,
    TaskEstimateFieldComponent,
    TaskPriorityFieldComponent,
    TaskProjectFieldComponent,
    TaskReporterFieldComponent,
    TaskSprintFieldComponent,
    TaskStatusPickerComponent,
  ],
  host: { class: 'block' },
  template: `
    @if (task(); as task) {
      @for (field of visibleFields(); track field) {
        @switch (field) {
          @case ('status') {
            @if (readStatus()) {
              <app-task-status-picker
                [buttonClass]="rowClass"
                [disabled]="!canUpdate()"
                [value]="task.statusId"
                (valueChange)="taskDetail.setStatus($event)">
                <span [class]="labelClass()">{{ labels.status }}</span>
                <span class="flex items-center gap-2 font-medium">
                  <span
                    class="bg-primary h-[7px] w-[7px] shrink-0 rounded-full"></span>
                  {{ task.statusName }}
                </span>
              </app-task-status-picker>
            }
          }

          @case ('assignee') {
            @if (readMembers()) {
              <app-task-assignee-field
                [labelWidth]="labelWidth()"
                [disabled]="!canUpdate()"
                [assignees]="task.assignees"
                (toggled)="taskDetail.toggleAssignee($event)" />
            }
          }

          @case ('reporter') {
            <app-task-reporter-field
              [labelWidth]="labelWidth()"
              [name]="task.ownerUsername"
              [pictureUrl]="task.ownerPictureUrl"
              [isServiceAccount]="task.ownerIsServiceAccount ?? false" />
          }

          @case ('priority') {
            <app-task-priority-field
              [labelWidth]="labelWidth()"
              [disabled]="!canUpdate()"
              [value]="task.priority"
              (valueChange)="taskDetail.setPriority($event)" />
          }

          @case ('estimate') {
            <app-task-estimate-field
              [labelWidth]="labelWidth()"
              [disabled]="!canUpdate()"
              [estimateType]="task.estimateType"
              [estimateValue]="task.estimateValue"
              (estimateChange)="taskDetail.setEstimate($event)" />
          }

          @case ('startDate') {
            <app-task-date-field
              kind="start"
              [labelWidth]="labelWidth()"
              [disabled]="!canUpdate()"
              [value]="task.startDate ?? ''"
              (valueChange)="taskDetail.setStartDate($event)" />
          }

          @case ('dueDate') {
            <app-task-date-field
              kind="due"
              [labelWidth]="labelWidth()"
              [disabled]="!canUpdate()"
              [value]="task.dueDate ?? ''"
              (valueChange)="taskDetail.setDueDate($event)" />
          }

          @case ('project') {
            @if (readProjects()) {
              <app-task-project-field
                [labelWidth]="labelWidth()"
                [disabled]="!canUpdate()"
                [projectName]="task.projectName"
                [value]="task.projectId"
                (valueChange)="taskDetail.setProject($event)" />
            }
          }

          @case ('sprint') {
            @if (readSprints()) {
              <app-task-sprint-field
                [labelWidth]="labelWidth()"
                [disabled]="!canUpdate()"
                [projectId]="task.projectId"
                [sprintName]="task.sprintName"
                [value]="task.sprintId ?? null"
                (valueChange)="taskDetail.setSprint($event)" />
            }
          }
        }
      }

      @if (foldableFields().length) {
        <div class="bg-foreground/8 mx-3 my-2.5 h-px" aria-hidden="true"></div>
        <button
          type="button"
          class="hover:bg-hover text-muted hover:text-foreground flex h-9 w-full cursor-pointer items-center gap-2 rounded-[7px] px-3 text-left text-xs font-medium transition-colors"
          [attr.aria-expanded]="emptyExpanded()"
          (click)="emptyExpanded.set(!emptyExpanded())">
          <svg
            lucideChevronDown
            class="h-3.5 w-3.5 shrink-0 transition-transform"
            [class.-rotate-90]="!emptyExpanded()"></svg>
          {{ emptyFieldsSummary() }}
        </button>
      }
    }
  `,
})
export class TaskDetailFieldRowsComponent {
  readonly fields = input<TaskDetailField[]>(ALL_FIELDS);

  readonly foldEmptyFields = input(false);
  readonly labelWidth = input('w-24');

  readonly taskDetail = inject(TaskDetailService);

  readonly task = this.taskDetail.task;
  readonly emptyExpanded = signal(false);

  readonly rowClass = fieldRowClass;

  readonly canUpdate = hasPermission(PERMISSIONS.tasks.update);
  readonly readStatus = hasPermission(PERMISSIONS.statuses.read);
  readonly readSprints = hasPermission(PERMISSIONS.sprints.read);
  readonly readProjects = hasPermission(PERMISSIONS.projects.read);
  readonly readMembers = hasPermission(PERMISSIONS.members.read);

  readonly labels = {
    status: $localize`:Field heading for the task status:Status`,
  };

  readonly labelClass = computed(() => {
    return cn(fieldLabelClass, this.labelWidth());
  });

  private readonly emptyFields = computed<TaskDetailField[]>(() => {
    const task = this.task();

    if (!task) return [];

    const empty: TaskDetailField[] = [];

    if (!task.assignees.length) empty.push('assignee');
    if (task.priority === null) empty.push('priority');
    if (task.estimateValue === null) empty.push('estimate');
    if (!task.startDate) empty.push('startDate');
    if (!task.dueDate) empty.push('dueDate');
    if (!task.sprintId) empty.push('sprint');

    return empty;
  });

  readonly foldableFields = computed<TaskDetailField[]>(() => {
    if (!this.foldEmptyFields()) return [];

    const empty = new Set(this.emptyFields());

    return this.fields().filter((field) => empty.has(field));
  });

  readonly hiddenFields = computed<TaskDetailField[]>(() => {
    return this.emptyExpanded() ? [] : this.foldableFields();
  });

  readonly visibleFields = computed(() => {
    const hidden = new Set(this.hiddenFields());

    return this.fields().filter((field) => !hidden.has(field));
  });

  readonly emptyFieldsSummary = computed(() => {
    if (this.emptyExpanded()) {
      return $localize`:Collapses the rows for fields the task has no value for:Hide empty fields`;
    }

    const names = this.foldableFields().map((field) => this.fieldName(field));

    const list = names.join(', ');

    if (names.length === 1) {
      return $localize`:Reveals the row for the one field the task has no value for. FIELD is its name:1 empty field — ${list}:FIELD:`;
    }

    const count = names.length;

    return $localize`:Reveals the rows for fields the task has no value for. COUNT is how many there are, FIELDS lists their names:${count}:COUNT: empty fields — ${list}:FIELDS:`;
  });

  private fieldName(field: TaskDetailField) {
    switch (field) {
      case 'assignee':
        return $localize`:Lowercase field name listed in the empty-fields summary:assignee`;
      case 'priority':
        return $localize`:Lowercase field name listed in the empty-fields summary:priority`;
      case 'estimate':
        return $localize`:Lowercase field name listed in the empty-fields summary:estimate`;
      case 'startDate':
        return $localize`:Lowercase field name listed in the empty-fields summary:start date`;
      case 'dueDate':
        return $localize`:Lowercase field name listed in the empty-fields summary:due date`;
      case 'sprint':
        return $localize`:Lowercase field name listed in the empty-fields summary:sprint`;
      default:
        return field;
    }
  }
}
