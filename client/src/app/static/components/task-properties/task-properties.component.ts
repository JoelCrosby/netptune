import { Component, inject, input, model, output } from '@angular/core';
import { netptunePermissions } from '@core/auth/permissions';
import { EstimateType } from '@core/enums/estimate-type';
import { TaskPriority } from '@core/enums/task-priority';
import { AppUser } from '@core/models/appuser';
import { AssigneeViewModel } from '@core/models/view-models/board-view';
import { userResource } from '@core/resources/user.resource';
import { selectHasPermission } from '@core/store/auth/auth.selectors';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { UserSelectComponent } from '@static/components/user-select/user-select.component';
import { DatePickerComponent } from '@static/components/date-picker/date-picker.component';
import {
  TaskEstimate,
  TaskEstimateSelectComponent,
} from './task-estimate-select.component';
import { TaskPrioritySelectComponent } from './task-priority-select.component';
import { TaskProjectSelectComponent } from './task-project-select.component';
import { TaskSprintSelectComponent } from './task-sprint-select.component';
import { TaskStatusSelectComponent } from './task-status-select.component';

export interface TaskReporter {
  displayName: string;
  pictureUrl?: string | null;
  isServiceAccount?: boolean;
}

@Component({
  selector: 'app-task-properties',
  imports: [
    AvatarComponent,
    UserSelectComponent,
    TaskPrioritySelectComponent,
    TaskEstimateSelectComponent,
    TaskProjectSelectComponent,
    TaskSprintSelectComponent,
    TaskStatusSelectComponent,
    DatePickerComponent,
  ],
  template: `
    <div class="flex flex-col">
      @if (readMembers()) {
        <div>
          <h4 class="font-sm mt-4 mb-2 font-semibold">
            {{ multiple() ? 'Assignees' : 'Assignee' }}
          </h4>
          <app-user-select
            i18n-label="
              Shown in the assignee picker when a task has nobody assigned
            "
            label="Unassigned"
            [value]="assignees()"
            [options]="users()"
            [disabled]="!editable()"
            (selectChange)="toggleAssignee($event)" />
        </div>
      }

      @if (reporter(); as reporter) {
        <div>
          <h4 class="font-sm mt-4 mb-2 font-semibold">
            <span i18n="Field heading for the person who raised the task">
              Reporter
            </span>
          </h4>
          <div class="flex flex-row items-center rounded pl-2">
            <app-avatar
              size="sm"
              [name]="reporter.displayName"
              [imageUrl]="reporter.pictureUrl"
              [isServiceAccount]="reporter.isServiceAccount ?? false" />
            <small class="ml-2 text-sm font-medium">
              {{ reporter.displayName }}
            </small>
          </div>
        </div>
      }

      @if (readStatus()) {
        <div>
          <h4 class="font-sm mt-4 mb-2 font-semibold">
            <span i18n="Field heading for the task status">Status</span>
          </h4>
          <app-task-status-select
            [(value)]="statusId"
            [loading]="loading()"
            [disabled]="!editable()"
            [fallbackLabel]="statusLabel()" />
        </div>
      }

      <div>
        <h4 class="font-sm mt-4 mb-2 font-semibold">
          <span i18n="Field heading for the task priority">Priority</span>
        </h4>
        <app-task-priority-select
          [(value)]="priority"
          [disabled]="!editable()" />
      </div>

      <div>
        <h4 class="font-sm mt-4 mb-2 font-semibold">
          <span i18n="Field heading for the task effort estimate">
            Estimate
          </span>
        </h4>
        <app-task-estimate-select
          [estimateType]="estimateType()"
          [estimateValue]="estimateValue()"
          [disabled]="!editable()"
          (estimateChange)="estimateChange.emit($event)" />
      </div>

      <div>
        <h4 class="font-sm mt-4 mb-2 font-semibold">
          <span i18n="Field heading for the task start date">Start date</span>
        </h4>
        <app-date-picker
          appearance="flat"
          color="ghost"
          i18n-ariaLabel="Accessible label for the task start date picker"
          ariaLabel="Start date"
          buttonClass="justify-between"
          [disabled]="!editable()"
          [(value)]="startDate" />
      </div>

      <div>
        <h4 class="font-sm mt-4 mb-2 font-semibold">
          <span i18n="Field heading for the task due date">Due date</span>
        </h4>
        <app-date-picker
          appearance="flat"
          color="ghost"
          i18n-ariaLabel="Accessible label for the task due date picker"
          ariaLabel="Due date"
          buttonClass="justify-between"
          [disabled]="!editable()"
          [(value)]="dueDate" />
      </div>

      @if (readProjects() && showProject()) {
        <div>
          <h4 class="font-sm mt-4 mb-2 font-semibold">
            <span i18n="Field heading for the task's project">Project</span>
          </h4>
          <app-task-project-select
            [(value)]="projectId"
            [disabled]="!editable()" />
        </div>
      }

      @if (readSprints() && showSprint()) {
        <div>
          <h4 class="font-sm mt-4 mb-2 font-semibold">
            <span i18n="Field heading for the task's sprint">Sprint</span>
          </h4>
          <app-task-sprint-select
            [(value)]="sprintId"
            [projectId]="projectId()"
            [loading]="loading()"
            [disabled]="!editable()"
            [fallbackLabel]="sprintLabel()" />
        </div>
      }
    </div>
  `,
})
export class TaskPropertiesComponent {
  private readonly store = inject(Store);

  readonly statusId = model<number | null>(null);
  readonly priority = model<TaskPriority | null>(null);
  readonly estimateType = input<EstimateType | null>(null);
  readonly estimateValue = input<number | null>(null);
  readonly startDate = model('');
  readonly dueDate = model('');
  readonly projectId = model<number | null>(null);
  readonly sprintId = model<number | null>(null);
  readonly assignees = model<(AppUser | AssigneeViewModel)[]>([]);

  readonly reporter = input<TaskReporter | null>(null);
  readonly loading = input(false);
  readonly editable = input(true);
  readonly showProject = input(true);
  readonly showSprint = input(true);
  readonly multiple = input(true);
  readonly statusLabel = input('Default');
  readonly sprintLabel = input('No Sprint');

  readonly estimateChange = output<TaskEstimate>();

  readonly usersResource = userResource();

  readonly readStatus = this.store.selectSignal(
    selectHasPermission(netptunePermissions.statuses.read)
  );
  readonly readSprints = this.store.selectSignal(
    selectHasPermission(netptunePermissions.sprints.read)
  );
  readonly readProjects = this.store.selectSignal(
    selectHasPermission(netptunePermissions.projects.read)
  );
  readonly readMembers = this.store.selectSignal(
    selectHasPermission(netptunePermissions.members.read)
  );

  users() {
    return (this.usersResource.value()?.payload?.items ?? []).filter(
      (user) => !user.isPending
    );
  }

  toggleAssignee(user: AppUser) {
    const assignees = this.assignees();
    const selected = assignees.some((assignee) => assignee.id === user.id);

    if (!this.multiple()) {
      this.assignees.set(selected ? [] : [user]);
      return;
    }

    this.assignees.set(
      selected
        ? assignees.filter((assignee) => assignee.id !== user.id)
        : [...assignees, user]
    );
  }
}
