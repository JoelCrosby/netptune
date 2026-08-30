import { Component, computed, inject, input, signal } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { EstimateType, formatEstimate } from '@core/enums/estimate-type';
import {
  TaskPriority,
  taskPriorityColors,
  taskPriorityLabels,
} from '@core/enums/task-priority';
import { LucideChevronDown, LucideFlag } from '@lucide/angular';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { DatePickerComponent } from '@static/components/date-picker/date-picker.component';
import { UserSelectComponent } from '@static/components/user-select/user-select.component';
import { TaskEstimatePickerComponent } from '../pickers/task-estimate-picker.component';
import { TaskPriorityPickerComponent } from '../pickers/task-priority-picker.component';
import { TaskProjectPickerComponent } from '../pickers/task-project-picker.component';
import { TaskSprintPickerComponent } from '../pickers/task-sprint-picker.component';
import { TaskStatusPickerComponent } from '../pickers/task-status-picker.component';
import { FAINT, FIELD_LABEL, FIELD_ROW } from '../task-detail-styles';
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
    AvatarComponent,
    DatePickerComponent,
    UserSelectComponent,
    TaskEstimatePickerComponent,
    TaskPriorityPickerComponent,
    TaskProjectPickerComponent,
    TaskSprintPickerComponent,
    TaskStatusPickerComponent,
    LucideChevronDown,
    LucideFlag,
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
              <app-user-select
                [buttonClass]="rowClass"
                [disabled]="!canUpdate()"
                [value]="task.assignees"
                (selectChange)="taskDetail.toggleAssignee($event)">
                <span [class]="labelClass()">{{ labels.assignee }}</span>
                @if (task.assignees.length) {
                  <span class="flex min-w-0 items-center gap-1.5 font-medium">
                    @for (assignee of task.assignees; track assignee.id) {
                      <app-avatar
                        size="sm"
                        [tooltip]="true"
                        [name]="assignee.displayName"
                        [imageUrl]="assignee.pictureUrl"
                        [isServiceAccount]="
                          assignee.isServiceAccount ?? false
                        " />
                    }
                    <span class="truncate">{{ assigneeLabel() }}</span>
                  </span>
                } @else {
                  <span [class]="faint">{{ labels.unassigned }}</span>
                }
              </app-user-select>
            }
          }

          @case ('reporter') {
            <div [class]="staticRowClass">
              <span [class]="labelClass()">{{ labels.reporter }}</span>
              <span class="flex min-w-0 items-center gap-1.5 font-medium">
                <app-avatar
                  size="sm"
                  [tooltip]="false"
                  [name]="task.ownerUsername"
                  [imageUrl]="task.ownerPictureUrl"
                  [isServiceAccount]="task.ownerIsServiceAccount ?? false" />
                <span class="truncate">{{ task.ownerUsername }}</span>
              </span>
            </div>
          }

          @case ('priority') {
            <app-task-priority-picker
              [buttonClass]="rowClass"
              [disabled]="!canUpdate()"
              [value]="task.priority"
              (valueChange)="taskDetail.setPriority($event)">
              <span [class]="labelClass()">{{ labels.priority }}</span>
              @if (task.priority === null) {
                <span [class]="faint">{{ labels.notSet }}</span>
              } @else {
                <span
                  class="flex items-center gap-2 font-medium"
                  [class]="priorityColor(task.priority)">
                  <svg lucideFlag class="h-3.5 w-3.5"></svg>
                  {{ priorityLabel(task.priority) }}
                </span>
              }
            </app-task-priority-picker>
          }

          @case ('estimate') {
            <app-task-estimate-picker
              [buttonClass]="rowClass"
              [disabled]="!canUpdate()"
              [estimateType]="task.estimateType"
              [estimateValue]="task.estimateValue"
              (estimateChange)="taskDetail.setEstimate($event)">
              <span [class]="labelClass()">{{ labels.estimate }}</span>
              @if (estimateLabel(); as estimate) {
                <span class="font-medium">{{ estimate }}</span>
              } @else {
                <span [class]="faint">{{ labels.notSet }}</span>
              }
            </app-task-estimate-picker>
          }

          @case ('startDate') {
            <div [class]="dateRowClass">
              <span [class]="labelClass()">{{ labels.startDate }}</span>
              <app-date-picker
                class="min-w-0 flex-1"
                appearance="bare"
                [buttonClass]="dateButtonClass"
                [showLeadingIcon]="false"
                [showChevron]="false"
                [disabled]="!canUpdate()"
                i18n-placeholder="
                  Shown in place of a date the task does not have
                "
                placeholder="Not set"
                i18n-ariaLabel="Accessible label for the task start date picker"
                ariaLabel="Start date"
                [value]="task.startDate ?? ''"
                (valueChange)="taskDetail.setStartDate($event)" />
            </div>
          }

          @case ('dueDate') {
            <div [class]="dateRowClass">
              <span [class]="labelClass()">{{ labels.dueDate }}</span>
              <app-date-picker
                class="min-w-0 flex-1"
                appearance="bare"
                [buttonClass]="dateButtonClass"
                [showLeadingIcon]="false"
                [showChevron]="false"
                [disabled]="!canUpdate()"
                i18n-placeholder="
                  Shown in place of a date the task does not have
                "
                placeholder="Not set"
                i18n-ariaLabel="Accessible label for the task due date picker"
                ariaLabel="Due date"
                [value]="task.dueDate ?? ''"
                (valueChange)="taskDetail.setDueDate($event)" />
            </div>
          }

          @case ('project') {
            @if (readProjects()) {
              <app-task-project-picker
                [buttonClass]="rowClass"
                [disabled]="!canUpdate()"
                [value]="task.projectId"
                (valueChange)="taskDetail.setProject($event)">
                <span [class]="labelClass()">{{ labels.project }}</span>
                <span class="truncate font-medium">{{ task.projectName }}</span>
              </app-task-project-picker>
            }
          }

          @case ('sprint') {
            @if (readSprints()) {
              <app-task-sprint-picker
                [buttonClass]="rowClass"
                [disabled]="!canUpdate()"
                [projectId]="task.projectId"
                [value]="task.sprintId ?? null"
                (valueChange)="taskDetail.setSprint($event)">
                <span [class]="labelClass()">{{ labels.sprint }}</span>
                @if (task.sprintName) {
                  <span class="truncate font-medium">{{
                    task.sprintName
                  }}</span>
                } @else {
                  <span [class]="faint">{{ labels.noSprint }}</span>
                }
              </app-task-sprint-picker>
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

  readonly rowClass = FIELD_ROW;
  readonly staticRowClass = `${FIELD_ROW} cursor-default hover:bg-transparent`;
  readonly dateRowClass = `${FIELD_ROW} cursor-default`;
  readonly dateButtonClass = 'h-8 w-auto gap-2 px-0 text-[13px] font-medium';
  readonly faint = FAINT;

  readonly canUpdate = hasPermission(PERMISSIONS.tasks.update);
  readonly readStatus = hasPermission(PERMISSIONS.statuses.read);
  readonly readSprints = hasPermission(PERMISSIONS.sprints.read);
  readonly readProjects = hasPermission(PERMISSIONS.projects.read);
  readonly readMembers = hasPermission(PERMISSIONS.members.read);

  readonly labels = {
    status: $localize`:Field heading for the task status:Status`,
    assignee: $localize`:Field heading for the people a task is assigned to:Assignee`,
    reporter: $localize`:Field heading for the person who raised the task:Reporter`,
    priority: $localize`:Field heading for the task priority:Priority`,
    estimate: $localize`:Field heading for the task effort estimate:Estimate`,
    startDate: $localize`:Field heading for the task start date:Start date`,
    dueDate: $localize`:Field heading for the task due date:Due date`,
    project: $localize`:Field heading for the task's project:Project`,
    sprint: $localize`:Field heading for the task's sprint:Sprint`,
    unassigned: $localize`:Shown in the assignee picker when a task has nobody assigned:Unassigned`,
    noSprint: $localize`:Shown in place of a sprint name when a task has no sprint:No Sprint`,
    notSet: $localize`:Shown in place of a value the task does not have:Not set`,
  };

  readonly labelClass = computed(() => `${FIELD_LABEL} ${this.labelWidth()}`);

  readonly assigneeLabel = computed(() => {
    const assignees = this.task()?.assignees ?? [];

    if (assignees.length === 1) return assignees[0].displayName;

    return `${assignees.length}`;
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

  readonly estimateLabel = computed(() => {
    const task = this.task();

    if (!task || task.estimateValue === null) return '';

    return formatEstimate(
      task.estimateType ?? EstimateType.storyPoints,
      task.estimateValue
    );
  });

  protected priorityColor(priority: TaskPriority) {
    return taskPriorityColors[priority];
  }

  protected priorityLabel(priority: TaskPriority) {
    return taskPriorityLabels[priority];
  }

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
