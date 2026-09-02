import { Component, computed, input, model, output } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import {
  EstimateType,
  formatEstimate,
  TaskEstimate,
} from '@core/enums/estimate-type';
import {
  TaskPriority,
  taskPriorityColors,
  taskPriorityLabels,
} from '@core/enums/task-priority';
import {
  UserSelectOption,
  UserSelectValue,
} from '@core/models/view-models/user-select-option';
import { projectResource } from '@core/resources/project.resource';
import { sprintResource } from '@core/resources/sprint.resource';
import { LucideFlag } from '@lucide/angular';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { DatePickerComponent } from '@static/components/date-picker/date-picker.component';
import { UserSelectComponent } from '@static/components/user-select/user-select.component';
import { TaskEstimatePickerComponent } from '../task-detail-dialog/pickers/task-estimate-picker.component';
import { TaskPriorityPickerComponent } from '../task-detail-dialog/pickers/task-priority-picker.component';
import { TaskProjectPickerComponent } from '../task-detail-dialog/pickers/task-project-picker.component';
import { TaskSprintPickerComponent } from '../task-detail-dialog/pickers/task-sprint-picker.component';
import {
  EMPTY_VALUE,
  FIELD_LABEL,
  FIELD_ROW,
} from '../task-detail-dialog/task-detail-styles';

export interface CreateTaskReporter {
  displayName: string;
  pictureUrl?: string | null;
  isServiceAccount?: boolean;
}

// The rail the create dialog shows before the task exists. It renders the same rows as
// the task detail dialog, but bound to the form's own signals rather than a saved task.
@Component({
  selector: 'app-create-task-field-rows',
  imports: [
    AvatarComponent,
    DatePickerComponent,
    UserSelectComponent,
    TaskEstimatePickerComponent,
    TaskPriorityPickerComponent,
    TaskProjectPickerComponent,
    TaskSprintPickerComponent,
    LucideFlag,
  ],
  host: { class: 'block' },
  template: `
    @if (readMembers()) {
      <app-user-select
        [buttonClass]="rowClass"
        [disabled]="!editable()"
        [excludeServiceAccounts]="true"
        [value]="assignees()"
        (selectChange)="toggleAssignee($event)">
        <span [class]="labelClass">{{ labels.assignee }}</span>
        @if (assignees().length) {
          <span class="flex min-w-0 items-center gap-1.5 font-medium">
            @for (assignee of assignees(); track assignee.id) {
              <app-avatar
                size="sm"
                [tooltip]="true"
                [name]="assignee.displayName"
                [imageUrl]="assignee.pictureUrl"
                [isServiceAccount]="assignee.isServiceAccount ?? false" />
            }
            <span class="truncate">{{ assigneeLabel() }}</span>
          </span>
        } @else {
          <span [class]="emptyValue">{{ labels.unassigned }}</span>
        }
      </app-user-select>
    }

    @if (reporter(); as reporter) {
      <div [class]="staticRowClass">
        <span [class]="labelClass">{{ labels.reporter }}</span>
        <span class="flex min-w-0 items-center gap-1.5 font-medium">
          <app-avatar
            size="sm"
            [tooltip]="false"
            [name]="reporter.displayName"
            [imageUrl]="reporter.pictureUrl"
            [isServiceAccount]="reporter.isServiceAccount ?? false" />
          <span class="truncate">{{ reporter.displayName }}</span>
        </span>
      </div>
    }

    <app-task-priority-picker
      [buttonClass]="rowClass"
      [disabled]="!editable()"
      [(value)]="priority">
      <span [class]="labelClass">{{ labels.priority }}</span>
      @if (priority() === null) {
        <span [class]="emptyValue">{{ labels.notSet }}</span>
      } @else {
        <span
          class="flex items-center gap-2 font-medium"
          [class]="priorityColor(priority()!)">
          <svg lucideFlag class="h-3.5 w-3.5"></svg>
          {{ priorityLabel(priority()!) }}
        </span>
      }
    </app-task-priority-picker>

    @if (readProjects() && showProject()) {
      <app-task-project-picker
        [buttonClass]="rowClass"
        [disabled]="!editable()"
        [(value)]="projectId">
        <span [class]="labelClass">{{ labels.project }}</span>
        @if (projectName(); as name) {
          <span class="truncate font-medium">{{ name }}</span>
        } @else {
          <span [class]="emptyValue">{{ labels.chooseProject }}</span>
        }
      </app-task-project-picker>
    }

    @if (readSprints() && showSprint()) {
      <app-task-sprint-picker
        [buttonClass]="rowClass"
        [disabled]="!editable()"
        [projectId]="projectId()"
        [(value)]="sprintId">
        <span [class]="labelClass">{{ labels.sprint }}</span>
        @if (sprintName(); as name) {
          <span class="truncate font-medium">{{ name }}</span>
        } @else {
          <span [class]="emptyValue">{{ labels.noSprint }}</span>
        }
      </app-task-sprint-picker>
    }

    <app-task-estimate-picker
      [buttonClass]="rowClass"
      [disabled]="!editable()"
      [estimateType]="estimateType()"
      [estimateValue]="estimateValue()"
      (estimateChange)="estimateChange.emit($event)">
      <span [class]="labelClass">{{ labels.estimate }}</span>
      @if (estimateLabel(); as estimate) {
        <span class="font-medium">{{ estimate }}</span>
      } @else {
        <span [class]="emptyValue">{{ labels.notSet }}</span>
      }
    </app-task-estimate-picker>

    <div [class]="dateRowClass">
      <span [class]="labelClass">{{ labels.startDate }}</span>
      <app-date-picker
        class="min-w-0 flex-1"
        appearance="bare"
        [buttonClass]="dateButtonClass"
        [showLeadingIcon]="false"
        [showChevron]="false"
        [disabled]="!editable()"
        i18n-placeholder="Shown in place of a date the task does not have"
        placeholder="Not set"
        i18n-ariaLabel="Accessible label for the task start date picker"
        ariaLabel="Start date"
        [(value)]="startDate" />
    </div>

    <div [class]="dateRowClass">
      <span [class]="labelClass">{{ labels.dueDate }}</span>
      <app-date-picker
        class="min-w-0 flex-1"
        appearance="bare"
        [buttonClass]="dateButtonClass"
        [showLeadingIcon]="false"
        [showChevron]="false"
        [disabled]="!editable()"
        i18n-placeholder="Shown in place of a date the task does not have"
        placeholder="Not set"
        i18n-ariaLabel="Accessible label for the task due date picker"
        ariaLabel="Due date"
        [(value)]="dueDate" />
    </div>
  `,
})
export class CreateTaskFieldRowsComponent {
  readonly priority = model<TaskPriority | null>(null);
  readonly projectId = model<number | null>(null);
  readonly sprintId = model<number | null>(null);
  readonly startDate = model('');
  readonly dueDate = model('');
  readonly assignees = model<UserSelectValue[]>([]);

  readonly estimateType = input<EstimateType | null>(null);
  readonly estimateValue = input<number | null>(null);
  readonly reporter = input<CreateTaskReporter | null>(null);
  readonly editable = input(true);
  readonly showProject = input(true);
  readonly showSprint = input(true);

  readonly estimateChange = output<TaskEstimate>();

  readonly rowClass = FIELD_ROW;
  readonly staticRowClass = `${FIELD_ROW} cursor-default hover:bg-transparent`;
  readonly dateRowClass = `${FIELD_ROW} cursor-default`;
  readonly dateButtonClass = 'h-8 w-auto gap-2 px-0 text-[13px] font-medium';
  readonly labelClass = `${FIELD_LABEL} w-24`;
  readonly emptyValue = EMPTY_VALUE;

  readonly readSprints = hasPermission(PERMISSIONS.sprints.read);
  readonly readProjects = hasPermission(PERMISSIONS.projects.read);
  readonly readMembers = hasPermission(PERMISSIONS.members.read);

  private readonly projects = projectResource();
  private readonly sprints = sprintResource();

  readonly projectName = computed(() => {
    const projectId = this.projectId();
    const project = this.projects.value().find((item) => item.id === projectId);

    return project?.name ?? null;
  });

  readonly sprintName = computed(() => {
    const sprintId = this.sprintId();
    const sprint = this.sprints.value().find((item) => item.id === sprintId);

    return sprint?.name ?? null;
  });

  readonly labels = {
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
    chooseProject: $localize`:Shown in the project row of the create-task dialog before a project is picked:Choose a project`,
  };

  readonly assigneeLabel = computed(() => {
    const assignees = this.assignees();

    if (assignees.length === 1) return assignees[0].displayName;

    return `${assignees.length}`;
  });

  readonly estimateLabel = computed(() => {
    const value = this.estimateValue();

    if (value === null) return '';

    return formatEstimate(
      this.estimateType() ?? EstimateType.storyPoints,
      value
    );
  });

  protected priorityColor(priority: TaskPriority) {
    return taskPriorityColors[priority];
  }

  protected priorityLabel(priority: TaskPriority) {
    return taskPriorityLabels[priority];
  }

  protected toggleAssignee(user: UserSelectOption) {
    const assignees = this.assignees();
    const selected = assignees.some((assignee) => assignee.id === user.id);

    this.assignees.set(
      selected
        ? assignees.filter((assignee) => assignee.id !== user.id)
        : [...assignees, user]
    );
  }
}
