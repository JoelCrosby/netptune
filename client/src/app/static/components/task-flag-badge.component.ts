import { Component, computed, input } from '@angular/core';
import { LucideFlag } from '@lucide/angular';
import { TooltipDirective } from '../directives/tooltip.directive';

@Component({
  selector: 'app-task-flag-badge',
  imports: [LucideFlag, TooltipDirective],
  template: `
    @if (count()) {
      <span
        class="flex items-center gap-1 rounded-full bg-amber-400/15 px-2 py-0.5 text-xs font-medium text-amber-700"
        [attr.aria-label]="label()"
        [appTooltip]="label()">
        <svg lucideFlag size="13" aria-hidden="true"></svg>
        {{ count() }}
      </span>
    }
  `,
})
export class TaskFlagBadgeComponent {
  readonly count = input.required<number>();
  readonly names = input<readonly string[]>([]);
  readonly label = computed(() => {
    const names = this.names();

    return names.length
      ? names.join(', ')
      : `${this.count()} active ${this.count() === 1 ? 'flag' : 'flags'}`;
  });
}
