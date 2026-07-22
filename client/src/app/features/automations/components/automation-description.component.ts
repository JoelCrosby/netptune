import { Component, input } from '@angular/core';
import { Status } from '@core/models/status';
import { TaskStatusPillComponent } from '@static/components/task-status-pill.component';
import { AutomationCopySegment, statusLabel } from '../models/automation-copy';

@Component({
  selector: 'app-automation-description',
  imports: [TaskStatusPillComponent],
  template: `
    @for (segment of segments(); track $index) {
      @if (segment.type === 'status') {
        @let status = findStatus(segment.statusId);
        <app-task-status-pill
          [name]="status?.name ?? statusLabel(segment.statusId)"
          [color]="status?.color"
          [category]="status?.category ?? null" />
      } @else {
        {{ segment.text }}
      }
    }
  `,
})
export class AutomationDescriptionComponent {
  readonly segments = input.required<AutomationCopySegment[]>();
  readonly statuses = input<Status[]>([]);

  findStatus(statusId: number): Status | null {
    return this.statuses().find((status) => status.id === statusId) ?? null;
  }

  statusLabel(statusId: number): string {
    return statusLabel(statusId, this.statuses());
  }
}
