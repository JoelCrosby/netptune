import { Component, computed, inject } from '@angular/core';
import { TaskPriority } from '@app/core/enums/task-priority';
import { AppUser } from '@app/core/models/appuser';
import { AssigneeViewModel } from '@app/core/models/view-models/board-view';
import {
  selectRequiredDetailTask,
  selectTaskEditLoading,
} from '@app/core/store/tasks/tasks.selectors';
import { TaskEstimate } from '@app/static/components/task-properties/task-estimate-select.component';
import {
  TaskPropertiesComponent,
  TaskReporter,
} from '@app/static/components/task-properties/task-properties.component';
import { Store } from '@ngrx/store';
import { TaskDetailService } from './task-detail.service';

@Component({
  selector: 'app-task-detail-properties',
  imports: [TaskPropertiesComponent],
  template: `
    <app-task-properties
      [statusId]="task().statusId"
      [statusLabel]="task().statusName"
      [priority]="task().priority"
      [estimateType]="task().estimateType"
      [estimateValue]="task().estimateValue"
      [startDate]="task().startDate ?? ''"
      [dueDate]="task().dueDate ?? ''"
      [projectId]="task().projectId"
      [sprintId]="task().sprintId ?? null"
      [sprintLabel]="task().sprintName ?? 'No Sprint'"
      [assignees]="task().assignees"
      [reporter]="reporter()"
      [loading]="updateLoading()"
      (statusIdChange)="selectStatus($event)"
      (priorityChange)="selectPriority($event)"
      (estimateChange)="selectEstimate($event)"
      (startDateChange)="selectStartDate($event)"
      (dueDateChange)="selectDueDate($event)"
      (projectIdChange)="selectProject($event)"
      (sprintIdChange)="selectSprint($event)"
      (assigneesChange)="selectAssignees($event)" />
  `,
})
export class TaskDetailPropertiesComponent {
  readonly store = inject(Store);
  readonly taskDetailService = inject(TaskDetailService);
  readonly task = this.store.selectSignal(selectRequiredDetailTask);
  readonly updateLoading = this.store.selectSignal(selectTaskEditLoading);

  readonly reporter = computed<TaskReporter>(() => ({
    displayName: this.task().ownerUsername,
    pictureUrl: this.task().ownerPictureUrl,
    isServiceAccount: this.task().ownerIsServiceAccount,
  }));

  selectStatus(statusId: number | null) {
    if (statusId === null) return;
    this.taskDetailService.updateTask({ statusId });
  }

  selectPriority(priority: TaskPriority | null) {
    this.taskDetailService.updateTask({ priority });
  }

  selectEstimate({ estimateType, estimateValue }: TaskEstimate) {
    this.taskDetailService.updateTask({ estimateType, estimateValue });
  }

  selectStartDate(startDate: string) {
    this.taskDetailService.updateTask({ startDate: startDate || null });
  }

  selectDueDate(dueDate: string) {
    this.taskDetailService.updateTask({ dueDate: dueDate || null });
  }

  selectProject(projectId: number | null) {
    if (projectId === null) return;
    this.taskDetailService.updateTask({ projectId });
  }

  selectSprint(sprintId: number | null) {
    if (sprintId === null) {
      this.taskDetailService.clearSprint();
      return;
    }

    this.taskDetailService.assignSprint(sprintId);
  }

  selectAssignees(assignees: (AppUser | AssigneeViewModel)[]) {
    this.taskDetailService.updateTask({
      assigneeIds: assignees.map((assignee) => assignee.id),
    });
  }
}
