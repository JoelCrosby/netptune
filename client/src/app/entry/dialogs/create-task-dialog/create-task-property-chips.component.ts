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
import { statusResource } from '@core/resources/status.resource';
import {
  LucideChevronDown,
  LucideFlag,
  LucideFolder,
  LucideGauge,
  LucideTimer,
} from '@lucide/angular';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { cn } from '@static/components/button/button.variants';
import { DatePickerComponent } from '@static/components/date-picker/date-picker.component';
import { UserSelectComponent } from '@static/components/user-select/user-select.component';
import { TaskEstimatePickerComponent } from '../task-detail-dialog/pickers/task-estimate-picker.component';
import { TaskPriorityPickerComponent } from '../task-detail-dialog/pickers/task-priority-picker.component';
import { TaskProjectPickerComponent } from '../task-detail-dialog/pickers/task-project-picker.component';
import { TaskSprintPickerComponent } from '../task-detail-dialog/pickers/task-sprint-picker.component';
import { TaskStatusPickerComponent } from '../task-detail-dialog/pickers/task-status-picker.component';
import {
  CHIP_EMPTY,
  CHIP_SET,
  CHIP_STATUS,
} from '../task-detail-dialog/task-detail-styles';

@Component({
  selector: 'app-create-task-property-chips',
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
    LucideFolder,
    LucideGauge,
    LucideTimer,
  ],
  host: { class: 'contents' },
  template: `
    @if (readStatus()) {
      <app-task-status-picker
        [buttonClass]="statusClass()"
        [disabled]="!editable()"
        [(value)]="statusId">
        @if (statusName(); as statusName) {
          <span class="bg-primary h-[7px] w-[7px] shrink-0 rounded-full"></span>
          {{ statusName }}
        } @else {
          <span i18n="Prompt on the empty status control">Status</span>
        }
        <svg lucideChevronDown class="h-3.5 w-3.5 opacity-60"></svg>
      </app-task-status-picker>
    }

    @if (readMembers()) {
      <app-user-select
        [buttonClass]="assigneeClass()"
        [disabled]="!editable()"
        [excludeServiceAccounts]="true"
        [value]="assignees()"
        (selectChange)="toggleAssignee($event)">
        @if (assignees().length) {
          @for (assignee of assignees(); track assignee.id) {
            <app-avatar
              size="sm"
              [tooltip]="false"
              [name]="assignee.displayName"
              [imageUrl]="assignee.pictureUrl"
              [isServiceAccount]="assignee.isServiceAccount ?? false" />
          }
          <span class="truncate">{{ assigneeLabel() }}</span>
        } @else {
          <span i18n="Prompt on the empty assignee control">Assignee</span>
        }
      </app-user-select>
    }

    <app-task-priority-picker
      [buttonClass]="priority() === null ? chipEmpty : chipSet"
      [disabled]="!editable()"
      [(value)]="priority">
      @let selectedPriority = priority();
      <svg
        lucideFlag
        class="h-3.5 w-3.5"
        [class]="
          selectedPriority === null ? '' : priorityColor(selectedPriority)
        "></svg>
      @if (selectedPriority === null) {
        <span i18n="Prompt on the empty priority control">Priority</span>
      } @else {
        <span [class]="priorityColor(selectedPriority)">
          {{ priorityLabel(selectedPriority) }}
        </span>
      }
    </app-task-priority-picker>

    <app-date-picker
      appearance="bare"
      [buttonClass]="dateClass(startDate())"
      [showChevron]="false"
      [disabled]="!editable()"
      [max]="dueDate() || undefined"
      i18n-placeholder="Prompt on the empty start date control"
      placeholder="Start date"
      i18n-ariaLabel="Accessible label for the task start date picker"
      ariaLabel="Start date"
      [(value)]="startDate" />

    <app-date-picker
      appearance="bare"
      [buttonClass]="dateClass(dueDate())"
      [showChevron]="false"
      [disabled]="!editable()"
      [min]="startDate() || undefined"
      i18n-placeholder="Prompt on the empty due date control"
      placeholder="Due date"
      i18n-ariaLabel="Accessible label for the task due date picker"
      ariaLabel="Due date"
      [(value)]="dueDate" />

    <app-task-estimate-picker
      [buttonClass]="estimateValue() === null ? chipEmpty : chipSet"
      [disabled]="!editable()"
      [estimateType]="estimateType()"
      [estimateValue]="estimateValue()"
      (estimateChange)="estimateChange.emit($event)">
      <svg lucideGauge class="h-3.5 w-3.5"></svg>
      @if (estimateLabel(); as estimate) {
        {{ estimate }}
      } @else {
        <span i18n="Prompt on the empty estimate control">Estimate</span>
      }
    </app-task-estimate-picker>

    @if (readProjects() && showProject()) {
      <app-task-project-picker
        [buttonClass]="projectClass()"
        [disabled]="!editable()"
        [(value)]="projectId">
        <svg lucideFolder class="h-3.5 w-3.5"></svg>
        @if (projectName(); as projectName) {
          <span class="max-w-48 truncate">{{ projectName }}</span>
        } @else {
          <span i18n="Prompt on the empty project control">Project</span>
        }
      </app-task-project-picker>
    }

    @if (readSprints() && showSprint()) {
      <app-task-sprint-picker
        [buttonClass]="sprintId() === null ? chipEmpty : chipSet"
        [disabled]="!editable()"
        [projectId]="projectId()"
        [(value)]="sprintId">
        <svg lucideTimer class="h-3.5 w-3.5"></svg>
        @if (sprintName(); as sprintName) {
          <span class="max-w-48 truncate">{{ sprintName }}</span>
        } @else {
          <span i18n="Prompt on the empty sprint control">Sprint</span>
        }
      </app-task-sprint-picker>
    }
  `,
})
export class CreateTaskPropertyChipsComponent {
  readonly statusId = model<number | null>(null);
  readonly priority = model<TaskPriority | null>(null);
  readonly projectId = model<number | null>(null);
  readonly sprintId = model<number | null>(null);
  readonly startDate = model('');
  readonly dueDate = model('');
  readonly assignees = model<UserSelectValue[]>([]);

  readonly estimateType = input<EstimateType | null>(null);
  readonly estimateValue = input<number | null>(null);
  readonly editable = input(true);
  readonly showProject = input(true);
  readonly showSprint = input(true);
  readonly projectInvalid = input(false);

  readonly estimateChange = output<TaskEstimate>();

  readonly readStatus = hasPermission(PERMISSIONS.statuses.read);
  readonly readMembers = hasPermission(PERMISSIONS.members.read);
  readonly readProjects = hasPermission(PERMISSIONS.projects.read);
  readonly readSprints = hasPermission(PERMISSIONS.sprints.read);

  readonly chipSet = CHIP_SET;
  readonly chipEmpty = CHIP_EMPTY;

  private readonly statuses = statusResource();
  private readonly projects = projectResource();
  private readonly sprints = sprintResource();

  readonly statusName = computed(() => {
    const statusId = this.statusId();
    const status = this.statuses.value().find((item) => item.id === statusId);

    return status?.name ?? null;
  });

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

  readonly statusClass = computed(() => {
    return this.statusId() === null ? CHIP_EMPTY : CHIP_STATUS;
  });

  readonly assigneeClass = computed(() => {
    if (!this.assignees().length) return CHIP_EMPTY;

    return `${CHIP_SET} w-auto py-0 pr-[11px] pl-[5px]`;
  });

  readonly projectClass = computed(() => {
    if (this.projectInvalid()) return cn(CHIP_EMPTY, 'border-warn text-warn');

    return this.projectId() === null ? CHIP_EMPTY : CHIP_SET;
  });

  readonly assigneeLabel = computed(() => {
    const assignees = this.assignees();

    if (assignees.length === 1) return assignees[0].displayName;

    return `${assignees.length}`;
  });

  readonly estimateLabel = computed(() => {
    const estimateValue = this.estimateValue();

    if (estimateValue === null) return '';

    return formatEstimate(
      this.estimateType() ?? EstimateType.storyPoints,
      estimateValue
    );
  });

  protected dateClass(value: string) {
    const chip = value ? CHIP_SET : CHIP_EMPTY;

    return `${chip} h-[30px] w-auto`;
  }

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
