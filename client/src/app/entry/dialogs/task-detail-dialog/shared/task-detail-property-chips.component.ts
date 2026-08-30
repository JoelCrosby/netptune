import { Component, computed, inject, input } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { EstimateType, formatEstimate } from '@core/enums/estimate-type';
import {
  TaskPriority,
  taskPriorityColors,
  taskPriorityLabels,
} from '@core/enums/task-priority';
import { LucideChevronDown, LucideFlag, LucideGauge } from '@lucide/angular';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { DatePickerComponent } from '@static/components/date-picker/date-picker.component';
import { UserSelectComponent } from '@static/components/user-select/user-select.component';
import { TaskEstimatePickerComponent } from '../pickers/task-estimate-picker.component';
import { TaskPriorityPickerComponent } from '../pickers/task-priority-picker.component';
import { TaskProjectPickerComponent } from '../pickers/task-project-picker.component';
import { TaskSprintPickerComponent } from '../pickers/task-sprint-picker.component';
import { TaskStatusPickerComponent } from '../pickers/task-status-picker.component';
import {
  CHIP_EMPTY,
  CHIP_SET,
  CHIP_STATUS,
  META_CHIP,
} from '../task-detail-styles';
import { TaskDetailService } from '../task-detail.service';

@Component({
  selector: 'app-task-detail-property-chips',
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
    LucideGauge,
  ],
  host: { class: 'contents' },
  template: `
    @if (task(); as task) {
      @if (readStatus()) {
        <app-task-status-picker
          [buttonClass]="statusClass()"
          [disabled]="!canUpdate()"
          [value]="task.statusId"
          (valueChange)="taskDetail.setStatus($event)">
          <span class="bg-primary h-[7px] w-[7px] shrink-0 rounded-full"></span>
          {{ task.statusName }}
          @if (canUpdate()) {
            <svg lucideChevronDown class="h-3.5 w-3.5 opacity-60"></svg>
          }
        </app-task-status-picker>
      }

      @if (isMeta()) {
        <span class="text-muted" aria-hidden="true">·</span>
      }

      @if (readMembers()) {
        <app-user-select
          [buttonClass]="assigneeClass()"
          [disabled]="!canUpdate()"
          [value]="task.assignees"
          (selectChange)="taskDetail.toggleAssignee($event)">
          @if (task.assignees.length) {
            @for (assignee of task.assignees; track assignee.id) {
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

        @if (isMeta()) {
          <span class="text-muted" aria-hidden="true">·</span>
        }
      }

      <app-task-priority-picker
        [buttonClass]="priorityClass()"
        [disabled]="!canUpdate()"
        [value]="task.priority"
        (valueChange)="taskDetail.setPriority($event)">
        <svg
          lucideFlag
          class="h-3.5 w-3.5"
          [class]="
            task.priority === null ? '' : priorityColor(task.priority)
          "></svg>
        @if (task.priority === null) {
          <span i18n="Prompt on the empty priority control">Priority</span>
        } @else {
          <span [class]="priorityColor(task.priority)">
            {{ priorityLabel(task.priority) }}
          </span>
        }
      </app-task-priority-picker>

      @if (isMeta()) {
        <span class="text-muted" aria-hidden="true">·</span>

        @if (readProjects()) {
          <app-task-project-picker
            [buttonClass]="metaChip"
            [disabled]="!canUpdate()"
            [value]="task.projectId"
            (valueChange)="taskDetail.setProject($event)">
            {{ task.projectName }}
          </app-task-project-picker>
        }

        @if (readSprints()) {
          <span class="text-muted" aria-hidden="true">/</span>

          <app-task-sprint-picker
            [buttonClass]="metaChip"
            [disabled]="!canUpdate()"
            [projectId]="task.projectId"
            [value]="task.sprintId ?? null"
            (valueChange)="taskDetail.setSprint($event)">
            @if (task.sprintName) {
              {{ task.sprintName }}
            } @else {
              <span class="text-muted">
                <span
                  i18n="
                    Shown in place of a sprint name when a task has no sprint
                  ">
                  No Sprint
                </span>
              </span>
            }
          </app-task-sprint-picker>
        }
      } @else {
        <app-date-picker
          appearance="bare"
          [buttonClass]="dueDateClass()"
          [showChevron]="false"
          [disabled]="!canUpdate()"
          i18n-placeholder="Prompt on the empty due date control"
          placeholder="Due date"
          i18n-ariaLabel="Accessible label for the task due date picker"
          ariaLabel="Due date"
          [value]="task.dueDate ?? ''"
          (valueChange)="taskDetail.setDueDate($event)" />

        <app-task-estimate-picker
          [buttonClass]="estimateClass()"
          [disabled]="!canUpdate()"
          [estimateType]="task.estimateType"
          [estimateValue]="task.estimateValue"
          (estimateChange)="taskDetail.setEstimate($event)">
          <svg lucideGauge class="h-3.5 w-3.5"></svg>
          @if (estimateLabel(); as estimate) {
            {{ estimate }}
          } @else {
            <span i18n="Prompt on the empty estimate control">Estimate</span>
          }
        </app-task-estimate-picker>
      }
    }
  `,
})
export class TaskDetailPropertyChipsComponent {
  readonly variant = input<'bar' | 'meta'>('bar');

  readonly taskDetail = inject(TaskDetailService);

  readonly task = this.taskDetail.task;

  readonly canUpdate = hasPermission(PERMISSIONS.tasks.update);
  readonly readStatus = hasPermission(PERMISSIONS.statuses.read);
  readonly readMembers = hasPermission(PERMISSIONS.members.read);
  readonly readProjects = hasPermission(PERMISSIONS.projects.read);
  readonly readSprints = hasPermission(PERMISSIONS.sprints.read);

  readonly metaChip = META_CHIP;

  readonly isMeta = computed(() => this.variant() === 'meta');

  readonly statusClass = computed(() => {
    return this.isMeta()
      ? `${META_CHIP} bg-primary/18 hover:bg-primary/28 font-semibold`
      : CHIP_STATUS;
  });

  readonly assigneeClass = computed(() => {
    const empty = !this.task()?.assignees.length;

    if (this.isMeta()) {
      return `${META_CHIP} w-auto py-0 pr-2 pl-1`;
    }

    return empty ? CHIP_EMPTY : `${CHIP_SET} w-auto py-0 pr-[11px] pl-[5px]`;
  });

  readonly priorityClass = computed(() => {
    if (this.isMeta()) return META_CHIP;

    return this.task()?.priority === null ? CHIP_EMPTY : CHIP_SET;
  });

  readonly dueDateClass = computed(() => {
    const chip = this.task()?.dueDate ? CHIP_SET : CHIP_EMPTY;

    return `${chip} h-[30px] w-auto`;
  });

  readonly estimateClass = computed(() => {
    return this.task()?.estimateValue === null ? CHIP_EMPTY : CHIP_SET;
  });

  readonly assigneeLabel = computed(() => {
    const assignees = this.task()?.assignees ?? [];

    if (assignees.length === 1) return assignees[0].displayName;

    return `${assignees.length}`;
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
}
