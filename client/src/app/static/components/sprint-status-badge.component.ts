import { Component, computed, input } from '@angular/core';
import { SprintStatus, sprintStatusLabels } from '@core/enums/sprint-status';

const statusClasses: Record<SprintStatus, string> = {
  [SprintStatus.planning]:
    'bg-blue-100 text-blue-800 dark:bg-blue-500/15 dark:text-blue-300',
  [SprintStatus.active]:
    'bg-green-100 text-green-800 dark:bg-green-500/15 dark:text-green-300',
  [SprintStatus.completed]:
    'bg-neutral-100 text-neutral-700 dark:bg-neutral-500/15 dark:text-neutral-300',
  [SprintStatus.cancelled]:
    'bg-red-100 text-red-700 dark:bg-red-500/15 dark:text-red-300',
};

@Component({
  selector: 'app-sprint-status-badge',
  host: { class: 'inline-flex max-w-full items-center' },
  template: `
    <span
      class="rounded-sm px-2 py-0.5 text-xs font-semibold whitespace-nowrap"
      [class]="statusClass()">
      {{ label() }}
    </span>
  `,
})
export class SprintStatusBadgeComponent {
  readonly status = input.required<SprintStatus>();

  protected readonly statusClass = computed(
    () => statusClasses[this.status()] ?? statusClasses[SprintStatus.planning]
  );

  protected readonly label = computed(
    () => sprintStatusLabels[this.status()] ?? ''
  );
}
