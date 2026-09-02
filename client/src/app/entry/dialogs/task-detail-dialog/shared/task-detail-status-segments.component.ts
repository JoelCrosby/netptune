import { Component, inject } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { TaskStatusSegmentsComponent } from '../pickers/task-status-segments.component';
import { TaskDetailService } from '../task-detail.service';

@Component({
  selector: 'app-task-detail-status-segments',
  imports: [TaskStatusSegmentsComponent],
  host: { class: 'block' },
  template: `
    @if (readStatus() && task(); as task) {
      <app-task-status-segments
        [value]="task.statusId"
        [disabled]="!canUpdate()"
        (valueChange)="setStatus($event)" />
    }
  `,
})
export class TaskDetailStatusSegmentsComponent {
  readonly taskDetail = inject(TaskDetailService);

  readonly task = this.taskDetail.task;

  readonly canUpdate = hasPermission(PERMISSIONS.tasks.update);
  readonly readStatus = hasPermission(PERMISSIONS.statuses.read);

  protected setStatus(statusId: number | null) {
    if (statusId === null) return;

    this.taskDetail.setStatus(statusId);
  }
}
