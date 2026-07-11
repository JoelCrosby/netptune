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
  ],
  template: `
    <div class="flex flex-col">
      @if (readMembers()) {
        <div>
          <h4 class="font-sm mt-4 mb-2 font-semibold">
            {{ multiple() ? 'Assignees' : 'Assignee' }}
          </h4>
          <app-user-select
            label="Unassigned"
            [value]="assignees()"
            [options]="users()"
            (selectChange)="toggleAssignee($event)" />
        </div>
      }

      @if (reporter(); as reporter) {
        <div>
          <h4 class="font-sm mt-4 mb-2 font-semibold">Reporter</h4>
          <div class="flex flex-row items-center rounded pl-2">
            <app-avatar
              size="sm"
              [name]="reporter.displayName"
              [imageUrl]="reporter.pictureUrl" />
            <small class="ml-2 text-sm font-medium">
              {{ reporter.displayName }}
            </small>
          </div>
        </div>
      }

      @if (readStatus()) {
        <div>
          <h4 class="font-sm mt-4 mb-2 font-semibold">Status</h4>
          <app-task-status-select
            [(value)]="statusId"
            [loading]="loading()"
            [fallbackLabel]="statusLabel()" />
        </div>
      }

      <div>
        <h4 class="font-sm mt-4 mb-2 font-semibold">Priority</h4>
        <app-task-priority-select [(value)]="priority" />
      </div>

      <div>
        <h4 class="font-sm mt-4 mb-2 font-semibold">Estimate</h4>
        <app-task-estimate-select
          [estimateType]="estimateType()"
          [estimateValue]="estimateValue()"
          (estimateChange)="estimateChange.emit($event)" />
      </div>

      @if (readProjects() && showProject()) {
        <div>
          <h4 class="font-sm mt-4 mb-2 font-semibold">Project</h4>
          <app-task-project-select [(value)]="projectId" />
        </div>
      }

      @if (readSprints() && showSprint()) {
        <div>
          <h4 class="font-sm mt-4 mb-2 font-semibold">Sprint</h4>
          <app-task-sprint-select
            [(value)]="sprintId"
            [projectId]="projectId()"
            [loading]="loading()"
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
  readonly projectId = model<number | null>(null);
  readonly sprintId = model<number | null>(null);
  readonly assignees = model<(AppUser | AssigneeViewModel)[]>([]);

  readonly reporter = input<TaskReporter | null>(null);
  readonly loading = input(false);
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
