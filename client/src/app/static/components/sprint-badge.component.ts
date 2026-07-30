import { Component, computed, input } from '@angular/core';
import { SprintStatus } from '@core/enums/sprint-status';

const ACTIVE_CLASSES =
  'bg-green-100 text-green-800 dark:bg-green-500/15 dark:text-green-300';
const INACTIVE_CLASSES =
  'bg-neutral-100 text-neutral-700 dark:bg-neutral-500/15 dark:text-neutral-300';

@Component({
  selector: 'app-sprint-badge',
  host: { class: 'inline-flex max-w-full items-center' },
  template: `
    <span
      class="max-w-full truncate rounded-sm px-2 py-1 text-xs font-semibold"
      [class]="statusClasses()">
      {{ name() }}
    </span>
  `,
})
export class SprintBadgeComponent {
  readonly name = input.required<string>();
  readonly status = input<SprintStatus | null | undefined>();

  protected readonly statusClasses = computed(() => {
    return this.status() === SprintStatus.active
      ? ACTIVE_CLASSES
      : INACTIVE_CLASSES;
  });
}
