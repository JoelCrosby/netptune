import { Component, computed, input } from '@angular/core';
import { SprintStatus } from '@core/enums/sprint-status';

interface DaysChip {
  label: string;
  classes: string;
}

const msPerDay = 86_400_000;

const overdueClasses =
  'bg-red-100 text-red-700 dark:bg-red-500/15 dark:text-red-300';
const dueSoonClasses =
  'bg-orange-100 text-orange-700 dark:bg-orange-500/15 dark:text-orange-300';
const remainingClasses =
  'bg-neutral-100 text-neutral-600 dark:bg-neutral-500/15 dark:text-neutral-300';

@Component({
  selector: 'app-sprint-days-badge',
  host: { class: 'contents' },
  template: `
    @if (chip(); as chip) {
      <span
        class="shrink-0 rounded-sm px-2 py-0.5 text-xs font-medium whitespace-nowrap"
        [class]="chip.classes">
        {{ chip.label }}
      </span>
    }
  `,
})
export class SprintDaysBadgeComponent {
  readonly status = input.required<SprintStatus>();
  readonly endDate = input.required<string>();

  protected readonly chip = computed<DaysChip | null>(() => {
    if (this.status() !== SprintStatus.active) return null;

    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const end = new Date(this.endDate());
    end.setHours(0, 0, 0, 0);
    const diff = Math.ceil((end.getTime() - today.getTime()) / msPerDay);

    if (diff < 0) {
      return { label: `${Math.abs(diff)}d overdue`, classes: overdueClasses };
    }

    if (diff === 0) {
      return {
        label: $localize`:Chip shown when a sprint ends today:Due today`,
        classes: dueSoonClasses,
      };
    }

    return {
      label: `${diff}d left`,
      classes: diff <= 3 ? dueSoonClasses : remainingClasses,
    };
  });
}
