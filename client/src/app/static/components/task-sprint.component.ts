import { Component, input } from '@angular/core';
import { SprintStatus } from '@core/enums/sprint-status';
import { SprintBadgeComponent } from './sprint-badge.component';

@Component({
  selector: 'app-task-sprint',
  imports: [SprintBadgeComponent],
  template: `
    @if (name(); as sprintName) {
      <app-sprint-badge
        class="max-w-40"
        [name]="sprintName"
        [status]="status()" />
    } @else {
      <span
        class="text-muted text-sm"
        i18n="Shown when a task is not in any sprint">
        Backlog
      </span>
    }
  `,
})
export class TaskSprintComponent {
  readonly name = input<string | null | undefined>(null);
  readonly status = input<SprintStatus | null | undefined>(null);
}
