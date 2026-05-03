import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { SprintStatus } from '@core/enums/sprint-status';

@Component({
  selector: 'app-sprint-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'inline-flex max-w-full items-center' },
  template: `
    <span
      class="max-w-full truncate rounded-sm px-2 py-1 text-xs font-semibold"
      [class.bg-green-100]="status() === sprintStatus.active"
      [class.text-green-800]="status() === sprintStatus.active"
      [class.bg-neutral-100]="status() !== sprintStatus.active"
      [class.text-neutral-700]="status() !== sprintStatus.active">
      {{ name() }}
    </span>
  `,
})
export class SprintBadgeComponent {
  readonly name = input.required<string>();
  readonly status = input<SprintStatus | null | undefined>();

  protected readonly sprintStatus = SprintStatus;
}
