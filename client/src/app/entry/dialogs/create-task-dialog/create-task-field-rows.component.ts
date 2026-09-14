import { Component, computed, input, model, output } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { EstimateType, TaskEstimate } from '@core/enums/estimate-type';
import { TaskPriority } from '@core/enums/task-priority';
import {
  UserSelectOption,
  UserSelectValue,
} from '@core/models/view-models/user-select-option';
import { projectResource } from '@core/resources/project.resource';
import { sprintResource } from '@core/resources/sprint.resource';
import { TaskAssigneeFieldComponent } from '../task-detail-dialog/parts/fields/task-assignee-field.component';
import { TaskDateFieldComponent } from '../task-detail-dialog/parts/fields/task-date-field.component';
import { TaskEstimateFieldComponent } from '../task-detail-dialog/parts/fields/task-estimate-field.component';
import { TaskPriorityFieldComponent } from '../task-detail-dialog/parts/fields/task-priority-field.component';
import { TaskProjectFieldComponent } from '../task-detail-dialog/parts/fields/task-project-field.component';
import { TaskReporterFieldComponent } from '../task-detail-dialog/parts/fields/task-reporter-field.component';
import { TaskSprintFieldComponent } from '../task-detail-dialog/parts/fields/task-sprint-field.component';

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
    TaskAssigneeFieldComponent,
    TaskDateFieldComponent,
    TaskEstimateFieldComponent,
    TaskPriorityFieldComponent,
    TaskProjectFieldComponent,
    TaskReporterFieldComponent,
    TaskSprintFieldComponent,
  ],
  host: { class: 'block' },
  template: `
    @if (readMembers()) {
      <app-task-assignee-field
        [disabled]="!editable()"
        [assignees]="assignees()"
        (toggled)="toggleAssignee($event)" />
    }

    @if (reporter(); as reporter) {
      <app-task-reporter-field
        [name]="reporter.displayName"
        [pictureUrl]="reporter.pictureUrl"
        [isServiceAccount]="reporter.isServiceAccount ?? false" />
    }

    <app-task-priority-field [disabled]="!editable()" [(value)]="priority" />

    @if (readProjects() && showProject()) {
      <app-task-project-field
        [disabled]="!editable()"
        [projectName]="projectName()"
        [(value)]="projectId" />
    }

    @if (readSprints() && showSprint()) {
      <app-task-sprint-field
        [disabled]="!editable()"
        [projectId]="projectId()"
        [sprintName]="sprintName()"
        [(value)]="sprintId" />
    }

    <app-task-estimate-field
      [disabled]="!editable()"
      [estimateType]="estimateType()"
      [estimateValue]="estimateValue()"
      (estimateChange)="estimateChange.emit($event)" />

    <app-task-date-field
      kind="start"
      [disabled]="!editable()"
      [(value)]="startDate" />

    <app-task-date-field
      kind="due"
      [disabled]="!editable()"
      [(value)]="dueDate" />
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
